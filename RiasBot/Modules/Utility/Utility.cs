using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database;
using RiasBot.Services.Database.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RiasBot.Modules.Utility
{
    public partial class Utility : RiasModule
    {
        public readonly CommandHandler _ch;
        public readonly CommandService _service;
        public readonly DbService _db;

        public Utility(CommandHandler ch, CommandService service, DbService db)
        {
            _ch = ch;
            _service = service;
            _db = db;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Prefix([Remainder]string newPrefix = null)
        {
            var user = (IGuildUser)Context.User;
            if (newPrefix is null)
            {
                await ReplyAsync($"{user.Mention} the prefix on this server is {Format.Bold(_ch._prefix)}").ConfigureAwait(false);
            }
            else if (user.GuildPermissions.Administrator)
            {
                string oldPrefix = _ch._prefix;

                using (var db = _db.GetDbContext())
                {
                    var guild = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();

                    try
                    {
                        guild.Prefix = newPrefix;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        var prefix = new GuildConfig { GuildId = Context.Guild.Id, Prefix = newPrefix };
                        await db.Guilds.AddAsync(prefix).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                await Context.Channel.SendConfirmationEmbed($"{user.Mention} the prefix on this server was changed from {Format.Bold(oldPrefix)} to {Format.Bold(newPrefix)}").ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendConfirmationEmbed($"{user.Mention} you don't have {Format.Bold("Administration")} permission").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Invite()
        {
            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} invite me on your server: [invite]({(RiasBot.invite)})");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Donate()
        {
            await Context.Channel.SendConfirmationEmbed($"Support me! Support this project on Patreon: [Onii-chan](https://www.patreon.com/riasbot)");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Ping()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            stopwatch.Stop();
            await Context.Channel.SendConfirmationEmbed(":ping_pong:" + stopwatch.ElapsedMilliseconds + "ms").ConfigureAwait(false);
        }
    }
}
