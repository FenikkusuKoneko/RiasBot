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
        public class Warnings : RiasModule<WarningsService>
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
                var userRequiredPermissions = Service.CheckRequiredPermissions((SocketGuildUser) Context.User);
                if (userRequiredPermissions != WarningsService.PermissionRequired.NoPermission)
                {
                    await SendMissingPermissionsAsync("User", userRequiredPermissions);
                    return;
                }

                var botRequiredPermissions = Service.CheckRequiredPermissions(Context.CurrentGuildUser!);
                if (botRequiredPermissions != WarningsService.PermissionRequired.NoPermission)
                {
                    await SendMissingPermissionsAsync("Bot", botRequiredPermissions);
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

                var warningResult = await Service.AddWarningAsync(user, (SocketGuildUser) Context.User, reason);
                if (warningResult.LimitReached)
                {
                    await ReplyErrorAsync("UserWarningsLimit", 10);
                    return;
                }

                if (warningResult.Punishment != WarningsService.PunishmentMethod.NoPunishment)
                {
                    await ApplyWarnPunishmentAsync(user, warningResult.Punishment);
                    return;
                }

                var embed = new EmbedBuilder()
                    {
                        Color = RiasUtils.Yellow,
                        Title = GetText("Warn")
                    }.AddField(GetText("#Common_User"), user, true)
                    .AddField(GetText("#Common_Id"), user.Id.ToString(), true)
                    .AddField(GetText("WarningNumber"), warningResult.WarningNumber, true)
                    .AddField(GetText("Moderator"), Context.User, true)
                    .WithThumbnailUrl(user.GetRealAvatarUrl());
                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText("#Common_Reason"), reason, true);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild!.GetTextChannel(Service.GetModLogChannelId(Context.Guild) ?? 0);
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
                var warnings = Service.GetWarnings(Context.Guild!)
                    .GroupBy(x => x.UserId)
                    .Select(x =>
                    {
                        var user = Context.Guild!.GetUser(x.First().UserId);
                        return user;
                    }).Where(y => y != null)
                    .OrderBy(u => u.Username)
                    .ToArray();

                if (warnings.Length == 0)
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
                var warnings = Service.GetUserWarnings(user)
                    .Select(x =>
                        new
                        {
                            Moderator = Context.Guild!.GetUser(x.ModeratorId),
                            x.Reason
                        })
                    .ToArray();

                if (warnings.Length == 0)
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
                    var permsHumanized = WarningsService.PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText($"{x}Permission"))
                        .Humanize(GetText("#Common_Or").ToLowerInvariant());
                    await ReplyErrorAsync("ClearWarningUserNoPermissions", permsHumanized);
                    return;
                }

                var warnings = Service.GetUserWarnings(user);
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

                await Service.RemoveWarningAsync(warning);
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
                    var permsHumanized = WarningsService.PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText($"{x}Permission"))
                        .Humanize(GetText("#Common_Or").ToLowerInvariant());
                    await ReplyErrorAsync("ClearWarningUserNoPermissions", permsHumanized);
                    return;
                }

                var warnings = Service.GetUserWarnings(user);
                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync("UserNoWarnings", user);
                    return;
                }

                var moderatorWarnings = guildUser.GuildPermissions.Administrator ? warnings : warnings.Where(x => x.ModeratorId == guildUser.Id).ToArray();

                if (moderatorWarnings.Count == 0)
                {
                    await ReplyErrorAsync("ClearWarningNotModWarnings", user);
                    return;
                }

                await Service.RemoveWarningsAsync(moderatorWarnings);
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
                var guildDb = Service.GetGuildDb(Context.Guild!);
                if (guildDb is null)
                {
                    await ReplyErrorAsync("NoWarningPunishment");
                    return;
                }
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

                if (number == 0)
                {
                    await Service.SetWarningPunishmentAsync(Context.Guild!, 0, null);
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

                await Service.SetWarningPunishmentAsync(Context.Guild!, number, punishment);
                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Description = GetText("WarningPunishmentSet"),
                    }.AddField(GetText("Warnings"), number, true)
                    .AddField(GetText("Punishment"), punishment.Titleize(), true);

                await ReplyAsync(embed);
            }

            private async Task SendMissingPermissionsAsync(string userType, WarningsService.PermissionRequired permissions)
            {
                switch (permissions)
                {
                    case WarningsService.PermissionRequired.MuteKickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText($"{x}Permission"))
                            .Humanize(GetText("#Common_Or").ToLowerInvariant());
                        await ReplyErrorAsync($"Warning{userType}NoPermissionsDefault", permsHumanized, GetText("#Attribute_Permissions").ToLowerInvariant());
                        return;
                    }
                    case WarningsService.PermissionRequired.KickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText($"{x}Permission"))
                            .Humanize(GetText("#Common_Or").ToLowerInvariant());
                        await ReplyErrorAsync($"Warning{userType}NoPermissionsPunishment", permsHumanized, GetText("#Attribute_Permissions").ToLowerInvariant());
                        return;
                    }
                }

                var punishmentHumanized = Service.GetGuildDb(Context.Guild!)!.WarningPunishment.Humanize(LetterCasing.Title);
                var permHumanized = GetText($"{permissions.Humanize()}Permission");
                await ReplyErrorAsync($"Warning{userType}NoPermissionsPunishment", punishmentHumanized, permHumanized, GetText("#Attribute_Permission").ToLowerInvariant());
            }

            private async Task ApplyWarnPunishmentAsync(SocketGuildUser user, WarningsService.PunishmentMethod punishment)
            {
                switch (punishment)
                {
                    case WarningsService.PunishmentMethod.Mute:
                        await _muteService.MuteUserAsync(Context.Channel, Context.CurrentGuildUser!, user, GetText("WarningMute"));
                        break;
                    case WarningsService.PunishmentMethod.Kick:
                        await SendMessageAsync(user, "UserKicked", "KickedFrom", GetText("WarningKick"));
                        await user.KickAsync();
                        break;
                    case WarningsService.PunishmentMethod.Ban:
                        await SendMessageAsync(user, "UserBanned", "BannedFrom", GetText("WarningBan"));
                        await user.BanAsync();
                        break;
                    case WarningsService.PunishmentMethod.Softban:
                        await SendMessageAsync(user, "UserSoftBanned", "KickedFrom", GetText("WarningKick"));
                        await Context.Guild!.AddBanAsync(user, 7);
                        await Context.Guild.RemoveBanAsync(user);
                        break;
                    case WarningsService.PunishmentMethod.Pruneban:
                        await SendMessageAsync(user, "UserBanned", "banned_from", GetText("WarningBan"));
                        await Context.Guild!.AddBanAsync(user, 7);
                        break;
                }
            }

            private async Task SendMessageAsync(SocketGuildUser user, string moderationType, string fromWhere, string? reason)
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
                var modLogChannel = Context.Guild!.GetTextChannel(Service.GetModLogChannelId(Context.Guild) ?? 0);
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
        }
    }
}