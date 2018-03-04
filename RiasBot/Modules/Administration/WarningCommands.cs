using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Administration.Services;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class WarningCommands : RiasSubmodule<WarningService>
        {
            private DiscordSocketClient _client;
            public readonly CommandHandler _ch;
            public readonly DbService _db;

            public WarningCommands(DiscordSocketClient client, CommandHandler ch, DbService db)
            {
                _client = client;
                _ch = ch;
                _db = db;
            }

            [RiasCommand] [@Alias] [Description] [@Remarks]
            [RequireUserPermission(GuildPermission.KickMembers | GuildPermission.BanMembers)]
            [RequireBotPermission(GuildPermission.KickMembers | GuildPermission.BanMembers)]
            [RequireContext(ContextType.Guild)]
            public async Task Warning(IGuildUser user, [Remainder]string reason = null)
            {
                if (user is null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} I couldn't find the user.");
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id).ToList();

                    var warning = new Warnings { GuildId = Context.Guild.Id, UserId = user.Id, Reason = reason, Moderator = Context.User.Id};
                    await db.AddAsync(warning).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);

                    var embed = new EmbedBuilder().WithColor(RiasBot.color);
                    embed.WithTitle($"{user}");
                    embed.AddField("Warning", warnings.Count + 1);
                    if (reason != null)
                        embed.AddField("Reason", reason);

                    await ReplyAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task WarningList()
            {
                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id).ToList();

                    if (warnings.Count == 0)
                    {
                        await ReplyAsync($"{Context.User.Mention} No warned users.");
                    }
                    else
                    {
                        string[] warnUsers = new string[warnings.Count];
                        for (int i = 0; i < warnings.Count; i++)
                        {
                            var user = await Context.Guild.GetUserAsync(warnings[i].UserId).ConfigureAwait(false);
                            warnUsers[i] = $"{i + 1}. {user}";
                        }
                        await Context.Channel.SendPaginated(_client, "All warned users", warnUsers, 10);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task WarningLog([Remainder]IGuildUser user)
            {
                if (user is null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} I couldn't find the user.");
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id);
                    var warningsUser = warnings.Where(x => x.UserId == user.Id).ToList();

                    string[] reasons = new string[warningsUser.Count];
                    for (int i = 0; i < warningsUser.Count; i++)
                    {
                        var moderator = await Context.Guild.GetUserAsync(warningsUser[i].Moderator).ConfigureAwait(false);
                        reasons[i] = $"{i+1}. {warningsUser[i].Reason ?? "-"}\n{Format.Bold("Moderator:")} {moderator.Username}#{moderator.Discriminator}\n";
                    }
                    if (warningsUser.Count == 0)
                    {
                        await ReplyAsync($"{Context.User.Mention} {user} doesn't have any warning!");
                    }
                    else
                    {
                        await Context.Channel.SendPaginated(_client, $"All warnings for {user}", reasons, 5);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task WarningClear(IGuildUser user, int index)
            {
                if (user is null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} I couldn't find the user.");
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id).ToList();

                    if (warnings.Count == 0)
                    {
                        await ReplyAsync("The user doesn't have any warning.");
                        return;
                    }
                    if ((index - 1) < warnings.Count)
                    {
                        db.Remove(warnings[index - 1]);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        await ReplyAsync("Warning removed!");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task WarningClear(IGuildUser user, string all)
            {
                if (user is null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention} I couldn't find the user.");
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id).ToList();

                    if (warnings.Count == 0)
                    {
                        await ReplyAsync("The user doesn't have any warning.");
                    }
                    else
                    {
                        if (all == "all")
                        {
                            foreach (var warning in warnings)
                            {
                                db.Remove(warning);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            await ReplyAsync("All warnings removed!");
                        }
                    }
                }
            }
        }
    }
}
