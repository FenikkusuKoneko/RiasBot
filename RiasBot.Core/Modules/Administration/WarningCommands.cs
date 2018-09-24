using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Administration.Services;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class WarningCommands : RiasSubmodule<WarningService>
        {
            private readonly DbService _db;
            private readonly AdministrationService _adminService;
            private readonly InteractiveService _is;

            public WarningCommands(DbService db, AdministrationService adminService, InteractiveService interactiveService)
            {
                _db = db;
                _adminService = adminService;
                _is = interactiveService;
            }

            [RiasCommand] [@Alias] [Description] [@Remarks]
            [RequireUserPermission(GuildPermission.KickMembers | GuildPermission.BanMembers)]
            [RequireBotPermission(GuildPermission.KickMembers | GuildPermission.BanMembers)]
            [RequireContext(ContextType.Guild)]
            public async Task WarningAsync(IGuildUser user, [Remainder]string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;
                if (user.Id != Context.Guild.OwnerId)
                    await Context.Channel.SendErrorMessageAsync("You cannot warn the owner of the server.").ConfigureAwait(false);
                else if (user.GuildPermissions.Administrator)
                    await Context.Channel.SendErrorMessageAsync("You cannot warn an administrator.").ConfigureAwait(false);
                else
                {
                    if (_adminService.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                    {
                        await _service.WarnUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, Context.Message, reason);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("The user is above me in the hierarchy roles.").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task WarningListAsync()
            {
                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id).GroupBy(y => y.UserId).Select(z => z.FirstOrDefault()).ToList();

                    if (warnings.Count == 0)
                    {
                        await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} No warned users.");
                    }
                    else
                    {
                        var index = 0;
                        var warnUsers = new List<string>();
                        foreach (var warn in warnings)
                        {
                            var user = await Context.Guild.GetUserAsync(warnings[index].UserId).ConfigureAwait(false);
                            if (user != null)
                            {
                                warnUsers.Add($"{index + 1}. {user}");
                                index++;
                            }
                            else
                            {
                                db.Remove(warn);
                            }
                        }
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        if (warnUsers.Any(x => !string.IsNullOrEmpty(x)))
                        {
                            var pager = new PaginatedMessage
                            {
                                Title = "All warned users",
                                Color = new Color(RiasBot.GoodColor),
                                Pages = warnUsers,
                                Options = new PaginatedAppearanceOptions
                                {
                                    ItemsPerPage = 10,
                                    Timeout = TimeSpan.FromMinutes(1),
                                    DisplayInformationIcon = false,
                                    JumpDisplayOptions = JumpDisplayOptions.Never
                                }

                            };
                            await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                        }
                        else
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} No warned users.");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task WarningLogAsync([Remainder]IGuildUser user)
            {
                if (user is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.Message.Author.Mention} I couldn't find the user.");
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id);
                    var warningsUser = warnings.Where(x => x.UserId == user.Id).ToList();

                    var reasons = new List<string>();
                    for (var i = 0; i < warningsUser.Count; i++)
                    {
                        var moderator = await Context.Guild.GetUserAsync(warningsUser[i].Moderator).ConfigureAwait(false);
                        reasons.Add($"#{i+1} {warningsUser[i].Reason ?? "-"}\n{Format.Bold("Moderator:")} {moderator.Username}#{moderator.Discriminator}\n");
                    }
                    if (warningsUser.Count == 0)
                    {
                        await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} {user} doesn't have any warning!");
                    }
                    else
                    {
                        var pager = new PaginatedMessage
                        {
                            Title = $"All warnings for {user}",
                            Color = new Color(RiasBot.GoodColor),
                            Pages = reasons,
                            Options = new PaginatedAppearanceOptions
                            {
                                ItemsPerPage = 5,
                                Timeout = TimeSpan.FromMinutes(1),
                                DisplayInformationIcon = false,
                                JumpDisplayOptions = JumpDisplayOptions.Never
                            }

                        };
                        await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task WarningClearAsync(IGuildUser user, int index)
            {
                if (user is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.Message.Author.Mention} I couldn't find the user.");
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id).Where(y => y.UserId == user.Id).ToList();

                    if (warnings.Count == 0)
                    {
                        await Context.Channel.SendConfirmationMessageAsync("The user doesn't have any warning.");
                        return;
                    }
                    if ((index - 1) < warnings.Count)
                    {
                        db.Remove(warnings[index - 1]);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync("Warning removed!");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task WarningClearAsync(IGuildUser user, string all)
            {
                if (user is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.Message.Author.Mention} I couldn't find the user.");
                    return;
                }

                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Warnings.Where(x => x.GuildId == Context.Guild.Id).Where(y => y.UserId == user.Id).ToList();

                    if (warnings.Count == 0)
                    {
                        await Context.Channel.SendConfirmationMessageAsync("The user doesn't have any warning.");
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
                            await Context.Channel.SendConfirmationMessageAsync("All warnings removed!");
                        }
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            public async Task WarningPunishmentAsync([Remainder]string punishment)
            {
                var punish = punishment.ToLowerInvariant().Split(" ");
                try
                {
                    punishment = punish[1];
                }
                catch
                {
                    punishment = null;
                }
                Int32.TryParse(punish[0], out var warns);
                using (var db = _db.GetDbContext())
                {
                    if (warns <= 0)
                    {
                        if (String.IsNullOrEmpty(punishment))
                        {
                            var warnings = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                            try
                            {
                                warnings.WarnsPunishment = warns;
                                warnings.PunishmentMethod = null;
                                await db.SaveChangesAsync().ConfigureAwait(false);
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} no warning punishment will be applied in this server.");
                            }
                            catch
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} no warning punishment will be applied in this server.");
                            }
                        }
                    }
                    else
                    {
                        switch (punishment)
                        {
                            case "mute":
                                await _service.RegisterMuteWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "m":
                                await _service.RegisterMuteWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "kick":
                                await _service.RegisterKickWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "k":
                                await _service.RegisterKickWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "ban":
                                await _service.RegisterBanWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "b":
                                await _service.RegisterBanWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "softban":
                                await _service.RegisterSoftbanWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "sb":
                                await _service.RegisterSoftbanWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "pruneban":
                                await _service.RegisterPrunebanWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            case "pb":
                                await _service.RegisterPrunebanWarning(Context.Guild, (IGuildUser)Context.User, Context.Channel, warns);
                                break;
                            default:
                                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the punishment method introduced is not valid. Use {Format.Bold("mute, kick, ban, softban or pruneban")}");
                                break;
                        }
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            public async Task WarningPunishmentAsync()
            {
                using (var db = _db.GetDbContext())
                {
                    var warnings = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                    var warns = warnings?.WarnsPunishment ?? 0;
                    var punish = warnings?.PunishmentMethod;

                    if (warns > 0)
                    {
                        if (punish.Contains("mute"))
                            await Context.Channel.SendConfirmationMessageAsync($"The punishement method for warning in this server is:\n ~at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold(punish + "d")}~");
                        else
                            await Context.Channel.SendConfirmationMessageAsync($"The punishement method for warning in this server is:\n ~at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold(punish + "ed")}~");
                    }
                    else
                        await Context.Channel.SendErrorMessageAsync("No punishment for warnings applied in this server.");
                }
            }
        }
    }
}
