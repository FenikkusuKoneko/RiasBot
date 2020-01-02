using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq.Extensions;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Warnings")]
        public class Warnings : RiasModule
        {
            private readonly MuteService _muteService;
            private readonly InteractiveService _interactive;

            public Warnings(IServiceProvider services) : base(services)
            {
                _muteService = services.GetRequiredService<MuteService>();
                _interactive = services.GetRequiredService<InteractiveService>();
            }

            [Command("warn"), Context(ContextType.Guild)]
            public async Task WarnAsync(SocketGuildUser user, [Remainder] string? reason = null)
            {
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new Guilds {GuildId = Context.Guild!.Id});

                var userRequiredPermissions = CheckRequiredPermissions((SocketGuildUser) Context.User, guildDb);
                if (userRequiredPermissions != PermissionRequired.NoPermission)
                {
                    await SendMissingPermissionsAsync("User", userRequiredPermissions, guildDb);
                    return;
                }

                var botRequiredPermissions = CheckRequiredPermissions(Context.CurrentGuildUser!, guildDb);
                if (botRequiredPermissions != PermissionRequired.NoPermission)
                {
                    await SendMissingPermissionsAsync("Bot", botRequiredPermissions, guildDb);
                    return;
                }

                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("CannotWarnOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                var userWarnings = await DbContext.GetListAsync<Database.Models.Warnings>(x => x.GuildId == Context.Guild!.Id && x.UserId == user.Id);
                var warnsCount = userWarnings.Count;

                var applyPunishment = false;
                if (warnsCount + 1 >= guildDb.PunishmentWarningsRequired && guildDb.PunishmentWarningsRequired != 0)
                {
                    DbContext.RemoveRange(userWarnings);
                    await DbContext.SaveChangesAsync();
                    applyPunishment = true;
                }
                
                if (!applyPunishment && warnsCount >= 10)
                {
                    await ReplyErrorAsync("UserWarningsLimit", 10);
                    return;
                }

                Enum.TryParse<PunishmentMethod>(guildDb.WarningPunishment, true, out var punishment);
                
                if (applyPunishment && punishment != PunishmentMethod.NoPunishment)
                {
                    await ApplyWarnPunishmentAsync(user, punishment, guildDb);
                    return;
                }
                
                await DbContext.AddAsync(new Database.Models.Warnings
                {
                    GuildId = user.Guild.Id,
                    UserId = user.Id,
                    ModeratorId = Context.User.Id,
                    Reason = reason
                });
                
                await DbContext.SaveChangesAsync();
                var embed = new EmbedBuilder()
                    {
                        Color = RiasUtils.Yellow,
                        Title = GetText("Warn")
                    }.AddField(GetText("#Common_User"), user, true)
                    .AddField(GetText("#Common_Id"), user.Id.ToString(), true)
                    .AddField(GetText("WarningNumber"), warnsCount + 1, true)
                    .AddField(GetText("Moderator"), Context.User, true)
                    .WithThumbnailUrl(user.GetRealAvatarUrl());
                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText("#Common_Reason"), reason, true);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild!.GetTextChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentGuildUser!.GetPermissions(modLogChannel);
                    if (preconditions.ViewChannel && preconditions.SendMessages)
                        channel = modLogChannel;
                }

                await channel.SendMessageAsync(embed);
            }

            [Command("warnings"), Context(ContextType.Guild),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task WarningsAsync()
            {
                var warnings = (await DbContext.GetListAsync<Database.Models.Warnings>(x => x.GuildId == Context.Guild!.Id))
                    .GroupBy(x => x.UserId)
                    .Select(x =>
                    {
                        var user = Context.Guild!.GetUser(x.First().UserId);
                        return user;
                    }).Where(y => y != null)
                    .OrderBy(u => u.Username)
                    .ToList();

                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync("NoWarnedUsers");
                    return;
                }

                var index = 1;
                var pages = warnings.Batch(15, x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = GetText("WarnedUsers"),
                        Color = RiasUtils.ConfirmColor,
                        Description = string.Join("\n", x.Select(u => $"{index++}. {u} | {u.Id}")),
                        Footer = new EmbedFooterBuilder
                        {
                            Text = GetText("WarningListFooter", GetPrefix())
                        }
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }

            [Command("warnings"), Context(ContextType.Guild),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task WarningsAsync([Remainder] SocketGuildUser user)
            {
                var warnings = (await DbContext.GetListAsync<Database.Models.Warnings>(x => x.GuildId == user.Guild.Id && x.UserId == user.Id))
                    .Select(x =>
                        new
                        {
                            Moderator = Context.Guild!.GetUser(x.ModeratorId),
                            x.Reason
                        })
                    .ToList();

                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync("UserNoWarnings", user);
                    return;
                }

                var index = 1;
                var pages = warnings.Batch(5, x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = GetText("UserWarnings", user),
                        Color = RiasUtils.ConfirmColor,
                        Description = string.Join("\n",
                            x.Select(w => $"{index++}. {GetText("#Common_Reason")}: {w.Reason ?? "-"}\n" +
                                          $"{GetText("Moderator")}: {w.Moderator}\n"))
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }

            [Command("clearwarning"), Context(ContextType.Guild),
            Priority(1)]
            public async Task ClearWarningAsync(SocketGuildUser user, int warningIndex)
            {
                if (--warningIndex < 0)
                    return;

                var guildUser = (SocketGuildUser) Context.User;
                if (!(guildUser.GuildPermissions.Administrator
                      || guildUser.GuildPermissions.MuteMembers
                      || guildUser.GuildPermissions.KickMembers
                      || guildUser.GuildPermissions.BanMembers))
                {
                    var permsHumanized = PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText($"{x}Permission"))
                        .Humanize(GetText("#Common_Or").ToLowerInvariant());
                    await ReplyErrorAsync("ClearWarningUserNoPermissions", permsHumanized);
                    return;
                }

                var warnings = (await DbContext.GetListAsync<Database.Models.Warnings>(x => x.GuildId == user.Guild.Id && x.UserId == user.Id));
                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync("UserNoWarnings", user);
                    return;
                }

                if (warningIndex >= warnings.Count)
                {
                    await ReplyErrorAsync("ClearWarningIndexAbove", user);
                    return;
                }

                var warning = warnings[warningIndex];
                if (guildUser.Id != warning.ModeratorId && !guildUser.GuildPermissions.Administrator)
                {
                    await ReplyErrorAsync("ClearWarningNotUserWarning", user);
                    return;
                }

                DbContext.Remove(warning);
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("WarningCleared", user);
            }

            [Command("clearwarning"), Context(ContextType.Guild),
             Priority(0)]
            public async Task ClearWarningAsync(SocketGuildUser user, string all)
            {
                if (!string.Equals(all, "all", StringComparison.InvariantCultureIgnoreCase))
                    return;

                var guildUser = (SocketGuildUser) Context.User;
                if (!(guildUser.GuildPermissions.Administrator
                      || guildUser.GuildPermissions.MuteMembers
                      || guildUser.GuildPermissions.KickMembers
                      || guildUser.GuildPermissions.BanMembers))
                {
                    var permsHumanized = PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText($"{x}Permission"))
                        .Humanize(GetText("#Common_Or").ToLowerInvariant());
                    await ReplyErrorAsync("ClearWarningUserNoPermissions", permsHumanized);
                    return;
                }

                var warnings = (await DbContext.GetListAsync<Database.Models.Warnings>(x => x.GuildId == user.Guild.Id && x.UserId == user.Id));
                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync("UserNoWarnings", user);
                    return;
                }

                var moderatorWarnings = guildUser.GuildPermissions.Administrator ? warnings : warnings.Where(x => x.ModeratorId == guildUser.Id).ToList();

                if (moderatorWarnings.Count == 0)
                {
                    await ReplyErrorAsync("ClearWarningNotModWarnings", user);
                    return;
                }
                
                DbContext.RemoveRange(moderatorWarnings);
                await DbContext.SaveChangesAsync();
                if (guildUser.GuildPermissions.Administrator || moderatorWarnings.Count == warnings.Count)
                {
                    await ReplyConfirmationAsync("AllWarningsCleared", user);
                }
                else
                {
                    var warningsString = moderatorWarnings.Count > 1 ? GetText("Warnings") : GetText("Warning");
                    await ReplyConfirmationAsync("WarningsCleared", moderatorWarnings.Count, warningsString.ToLowerInvariant(), user);
                }
            }

            [Command("warningpunishment"), Context(ContextType.Guild)]
            public async Task WarningPunishmentAsync()
            {
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new Guilds {GuildId = Context.Guild!.Id});
                if (guildDb.PunishmentWarningsRequired == 0)
                {
                    await ReplyErrorAsync("NoWarningPunishment");
                    return;
                }

                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Description = GetText("WarningPunishment"),
                    }.AddField(GetText("Warnings"), guildDb.PunishmentWarningsRequired, true)
                    .AddField(GetText("Punishment"), guildDb.WarningPunishment.Titleize(), true);

                await ReplyAsync(embed);
            }

            [Command("setwarningpunishment"), Context(ContextType.Guild),
            UserPermission(GuildPermission.Administrator)]
            public async Task SetWarningPunishmentAsync(int number, string? punishment = null)
            {
                if (number < 0)
                    return;

                if (number > 10)
                {
                    await ReplyErrorAsync("WarningsLimit", 10);
                    return;
                }

                Guilds guildDb;
                if (number == 0)
                {
                    guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new Guilds {GuildId = Context.Guild!.Id});
                    guildDb.PunishmentWarningsRequired = 0;
                    guildDb.WarningPunishment = null;

                    await DbContext.SaveChangesAsync();
                    await ReplyConfirmationAsync("WarningPunishmentRemoved");
                    return;
                }

                punishment = punishment?.ToLowerInvariant();
                if (!(string.Equals(punishment, "mute")
                      || string.Equals(punishment, "kick")
                      || string.Equals(punishment, "ban")
                      || string.Equals(punishment, "softban")
                      || string.Equals(punishment, "pruneban")))
                {
                    await ReplyErrorAsync("WarningInvalidPunishment");
                    return;
                }
                
                guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new Guilds {GuildId = Context.Guild!.Id});
                guildDb.PunishmentWarningsRequired = number;
                guildDb.WarningPunishment = punishment;

                await DbContext.SaveChangesAsync();
                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Description = GetText("WarningPunishmentSet"),
                    }.AddField(GetText("Warnings"), number, true)
                    .AddField(GetText("Punishment"), punishment.Titleize(), true);

                await ReplyAsync(embed);
            }
            
            private PermissionRequired CheckRequiredPermissions(SocketGuildUser user, Guilds guildDb)
            {
                if (user.Id == user.Guild.OwnerId || user.GuildPermissions.Administrator)
                    return PermissionRequired.NoPermission;

                var warnPunishment = guildDb?.WarningPunishment;
                if (string.IsNullOrEmpty(warnPunishment))
                    return user.GuildPermissions.MuteMembers || user.GuildPermissions.KickMembers || user.GuildPermissions.BanMembers
                        ? PermissionRequired.NoPermission
                        : PermissionRequired.MuteKickBan;

                if (string.Equals(warnPunishment, "mute", StringComparison.InvariantCultureIgnoreCase))
                    return user.GuildPermissions.MuteMembers ? PermissionRequired.NoPermission : PermissionRequired.Mute;

                if (string.Equals(warnPunishment, "kick", StringComparison.InvariantCultureIgnoreCase))
                    return user.GuildPermissions.KickMembers ? PermissionRequired.NoPermission : PermissionRequired.Kick;

                if (string.Equals(warnPunishment, "ban", StringComparison.InvariantCultureIgnoreCase))
                    return user.GuildPermissions.BanMembers ? PermissionRequired.NoPermission : PermissionRequired.Ban;

                if (string.Equals(warnPunishment, "softban", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (user.Id == user.Guild.CurrentUser.Id)
                    {
                        return user.GuildPermissions.KickMembers && user.GuildPermissions.BanMembers
                            ? PermissionRequired.NoPermission
                            : PermissionRequired.KickBan;
                    }

                    return user.GuildPermissions.KickMembers ? PermissionRequired.NoPermission : PermissionRequired.Kick;
                }

                if (string.Equals(warnPunishment, "pruneban", StringComparison.InvariantCultureIgnoreCase))
                    return user.GuildPermissions.BanMembers ? PermissionRequired.NoPermission : PermissionRequired.Ban;

                return default;
            }

            private async Task SendMissingPermissionsAsync(string userType, PermissionRequired permissions, Guilds guildDb)
            {
                switch (permissions)
                {
                    case PermissionRequired.MuteKickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText($"{x}Permission"))
                            .Humanize(GetText("#Common_Or").ToLowerInvariant());
                        await ReplyErrorAsync($"Warning{userType}NoPermissionsDefault", permsHumanized, GetText("#Attribute_Permissions").ToLowerInvariant());
                        return;
                    }
                    case PermissionRequired.KickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText($"{x}Permission"))
                            .Humanize(GetText("#Common_Or").ToLowerInvariant());
                        await ReplyErrorAsync($"Warning{userType}NoPermissionsPunishment", permsHumanized, GetText("#Attribute_Permissions").ToLowerInvariant());
                        return;
                    }
                }
                
                var punishmentHumanized = guildDb.WarningPunishment.Humanize(LetterCasing.Title);
                var permHumanized = GetText($"{permissions.Humanize()}Permission");
                await ReplyErrorAsync($"Warning{userType}NoPermissionsPunishment", punishmentHumanized, permHumanized, GetText("#Attribute_Permission").ToLowerInvariant());
            }

            private async Task ApplyWarnPunishmentAsync(SocketGuildUser user, PunishmentMethod punishment, Guilds guildDb)
            {
                switch (punishment)
                {
                    case PunishmentMethod.Mute:
                        await _muteService.MuteUserAsync(Context.Channel, Context.CurrentGuildUser!, user, GetText("WarningMute"));
                        break;
                    case PunishmentMethod.Kick:
                        await SendMessageAsync(user, guildDb, "UserKicked", "KickedFrom", GetText("WarningKick"));
                        await user.KickAsync();
                        break;
                    case PunishmentMethod.Ban:
                        await SendMessageAsync(user, guildDb, "UserBanned", "BannedFrom", GetText("WarningBan"));
                        await user.BanAsync();
                        break;
                    case PunishmentMethod.SoftBan:
                        await SendMessageAsync(user, guildDb, "UserSoftBanned", "KickedFrom", GetText("WarningKick"));
                        await Context.Guild!.AddBanAsync(user, 7);
                        await Context.Guild.RemoveBanAsync(user);
                        break;
                    case PunishmentMethod.PruneBan:
                        await SendMessageAsync(user, guildDb, "UserBanned", "banned_from", GetText("WarningBan"));
                        await Context.Guild!.AddBanAsync(user, 7);
                        break;
                }
            }

            private async Task SendMessageAsync(SocketGuildUser user, Guilds guildDb, string moderationType, string fromWhere, string? reason)
            {
                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ErrorColor,
                        Title = GetText(moderationType),
                        ThumbnailUrl = user.GetRealAvatarUrl()
                    }.AddField(GetText("#Common_User"), user, true)
                    .AddField(GetText("#Common_Id"), user.Id.ToString(), true)
                    .AddField(GetText("Moderator"), Context.User, true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText("#Common_Reason"), reason, true);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild!.GetTextChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentGuildUser!.GetPermissions(modLogChannel);
                    if (preconditions.ViewChannel && preconditions.SendMessages)
                        channel = modLogChannel;
                }

                if (channel.Id != Context.Channel.Id)
                    await Context.Message.AddReactionAsync(new Emoji("✅"));

                await channel.SendMessageAsync(embed);

                var reasonEmbed = new EmbedBuilder
                {
                    Color = RiasUtils.ErrorColor,
                    Description = GetText(fromWhere, Context.Guild.Name)
                };

                if (!string.IsNullOrEmpty(reason))
                    reasonEmbed.AddField(GetText("#Common_Reason"), reason, true);

                try
                {
                    if (!user.IsBot)
                        await user.SendMessageAsync(reasonEmbed);
                }
                catch
                {
                    // the user blocked the messages from the guild users
                }
            }
            
            private enum PermissionRequired
            {
                NoPermission,
                MuteKickBan,
                Mute,
                Kick,
                Ban,
                KickBan
            }

            private enum PunishmentMethod
            {
                NoPunishment,
                Mute,
                Kick,
                Ban,
                SoftBan,
                PruneBan
            }
        }
    }
}