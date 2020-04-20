using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Warnings")]
        public class WarningsSubmodule : RiasModule
        {
            private readonly MuteService _muteService;
            
            public WarningsSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                _muteService = serviceProvider.GetRequiredService<MuteService>();
            }
            
            [Command("warn"), Context(ContextType.Guild)]
            public async Task WarnAsync(CachedMember member, [Remainder] string? reason = null)
            {
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});

                var userRequiredPermissions = CheckRequiredPermissions((CachedMember) Context.User, guildDb);
                if (userRequiredPermissions != PermissionRequired.NoPermission)
                {
                    await SendMissingPermissionsAsync("user", userRequiredPermissions, guildDb);
                    return;
                }

                var botRequiredPermissions = CheckRequiredPermissions(Context.CurrentMember!, guildDb);
                if (botRequiredPermissions != PermissionRequired.NoPermission)
                {
                    await SendMissingPermissionsAsync("bot", botRequiredPermissions, guildDb);
                    return;
                }

                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotWarnOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                var userWarnings = await DbContext.GetListAsync<WarningsEntity>(x => x.GuildId == Context.Guild!.Id && x.UserId == member.Id);
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
                    await ReplyErrorAsync(Localization.AdministrationUserWarningsLimit, 10);
                    return;
                }

                Enum.TryParse<PunishmentMethod>(guildDb.WarningPunishment, true, out var punishment);
                
                if (applyPunishment && punishment != PunishmentMethod.NoPunishment)
                {
                    await ApplyWarnPunishmentAsync(member, punishment, guildDb);
                    return;
                }
                
                await DbContext.AddAsync(new WarningsEntity
                {
                    GuildId = member.Guild.Id,
                    UserId = member.Id,
                    ModeratorId = Context.User.Id,
                    Reason = reason
                });
                
                await DbContext.SaveChangesAsync();
                var embed = new LocalEmbedBuilder()
                    {
                        Color = RiasUtilities.Yellow,
                        Title = GetText(Localization.AdministrationWarn)
                    }.AddField(GetText(Localization.CommonUser), member, true)
                    .AddField(GetText(Localization.CommonId), member.Id.ToString(), true)
                    .AddField(GetText(Localization.AdministrationWarningNumber), warnsCount + 1, true)
                    .AddField(GetText(Localization.AdministrationModerator), Context.User, true)
                    .WithThumbnailUrl(member.GetAvatarUrl());
                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText(Localization.CommonReason), reason, true);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild!.GetTextChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentMember!.GetPermissionsFor(modLogChannel);
                    if (preconditions.ViewChannel && preconditions.SendMessages)
                        channel = modLogChannel;
                }

                await channel.SendMessageAsync(embed);
            }
            
            [Command("warnings"), Context(ContextType.Guild),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task WarningsAsync()
            {
                var warnings = (await DbContext.GetListAsync<WarningsEntity>(x => x.GuildId == Context.Guild!.Id))
                    .GroupBy(x => x.UserId)
                    .Select(x => Context.Guild!.GetMember(x.First().UserId)).Where(y => y != null)
                    .OrderBy(u => u.Name)
                    .ToList();

                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationNoWarnedUsers);
                    return;
                }
                
                await SendPaginatedMessageAsync(warnings, 15, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationWarnedUsers),
                    Description = string.Join("\n", items.Select(x => $"{++index}. {x} | {x.Id}")),
                    Footer = new LocalEmbedFooterBuilder().WithText(GetText(Localization.AdministrationWarningListFooter, Context.Prefix))
                });
            }
            
            [Command("warnings"), Context(ContextType.Guild),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task WarningsAsync([Remainder] CachedMember member)
            {
                var warnings = (await DbContext.GetListAsync<WarningsEntity>(x => x.GuildId == member.Guild.Id && x.UserId == member.Id))
                    .Select(x => new
                    {
                        Moderator = Context.Guild!.GetMember(x.ModeratorId),
                        x.Reason
                    })
                    .ToList();

                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNoWarnings, member);
                    return;
                }
                
                await SendPaginatedMessageAsync(warnings, 5, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationUserWarnings, member),
                    Description = string.Join("\n",
                        items.Select(x => $"{++index}. {GetText(Localization.CommonReason)}: {x.Reason ?? "-"}\n" +
                                          $"{GetText(Localization.AdministrationModerator)}: {x.Moderator}\n"))
                });
            }
            
            [Command("clearwarning"), Context(ContextType.Guild),
             Priority(1)]
            public async Task ClearWarningAsync(CachedMember member, int warningIndex)
            {
                if (--warningIndex < 0)
                    return;

                var commandUser = (CachedMember) Context.User;
                if (!(commandUser.Permissions.Administrator
                      || commandUser.Permissions.MuteMembers
                      || commandUser.Permissions.KickMembers
                      || commandUser.Permissions.BanMembers))
                {
                    var permsHumanized = PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                        .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                    await ReplyErrorAsync(Localization.AdministrationClearWarningUserNoPermissions, permsHumanized);
                    return;
                }

                var warnings = (await DbContext.GetListAsync<WarningsEntity>(x => x.GuildId == member.Guild.Id && x.UserId == member.Id));
                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNoWarnings, member);
                    return;
                }

                if (warningIndex >= warnings.Count)
                {
                    await ReplyErrorAsync(Localization.AdministrationClearWarningIndexAbove, member);
                    return;
                }

                var warning = warnings[warningIndex];
                if (commandUser.Id != warning.ModeratorId && !commandUser.Permissions.Administrator)
                {
                    await ReplyErrorAsync(Localization.AdministrationClearWarningNotUserWarning, member);
                    return;
                }

                DbContext.Remove(warning);
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.AdministrationWarningCleared, member);
            }
            
            [Command("clearwarning"), Context(ContextType.Guild),
             Priority(0)]
            public async Task ClearWarningAsync(CachedMember member, string all)
            {
                if (!string.Equals(all, "all", StringComparison.InvariantCultureIgnoreCase))
                    return;

                var commandMember = (CachedMember) Context.User;
                if (!(commandMember.Permissions.Administrator
                      || commandMember.Permissions.MuteMembers
                      || commandMember.Permissions.KickMembers
                      || commandMember.Permissions.BanMembers))
                {
                    var permsHumanized = PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                        .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                    await ReplyErrorAsync(Localization.AdministrationClearWarningUserNoPermissions, permsHumanized);
                    return;
                }

                var warnings = (await DbContext.GetListAsync<WarningsEntity>(x => x.GuildId == member.Guild.Id && x.UserId == member.Id));
                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNoWarnings, member);
                    return;
                }

                var moderatorWarnings = commandMember.Permissions.Administrator ? warnings : warnings.Where(x => x.ModeratorId == commandMember.Id).ToList();
                if (moderatorWarnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationClearWarningNotModWarnings, member);
                    return;
                }
                
                DbContext.RemoveRange(moderatorWarnings);
                await DbContext.SaveChangesAsync();
                if (commandMember.Permissions.Administrator || moderatorWarnings.Count == warnings.Count)
                {
                    await ReplyConfirmationAsync(Localization.AdministrationAllWarningsCleared, member);
                }
                else
                {
                    var warningsString = moderatorWarnings.Count > 1 ? GetText(Localization.AdministrationWarnings) : GetText(Localization.AdministrationWarning);
                    await ReplyConfirmationAsync(Localization.AdministrationWarningsCleared, moderatorWarnings.Count, warningsString.ToLowerInvariant(), member);
                }
            }
            
            [Command("warningpunishment"), Context(ContextType.Guild)]
            public async Task WarningPunishmentAsync()
            {
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                if (guildDb.PunishmentWarningsRequired == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationNoWarningPunishment);
                    return;
                }

                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Description = GetText(Localization.AdministrationWarningPunishment)
                    }.AddField(GetText(Localization.AdministrationWarnings), guildDb.PunishmentWarningsRequired, true)
                    .AddField(GetText(Localization.AdministrationPunishment), guildDb.WarningPunishment.Titleize(), true);

                await ReplyAsync(embed);
            }

            [Command("setwarningpunishment"), Context(ContextType.Guild),
            UserPermission(Permission.Administrator)]
            public async Task SetWarningPunishmentAsync(int number, string? punishment = null)
            {
                if (number < 0)
                    return;

                if (number > 10)
                {
                    await ReplyErrorAsync(Localization.AdministrationWarningsLimit, 10);
                    return;
                }

                GuildsEntity guildDb;
                if (number == 0)
                {
                    guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                    guildDb.PunishmentWarningsRequired = 0;
                    guildDb.WarningPunishment = null;

                    await DbContext.SaveChangesAsync();
                    await ReplyConfirmationAsync(Localization.AdministrationWarningPunishmentRemoved);
                    return;
                }

                punishment = punishment?.ToLowerInvariant();
                if (!(string.Equals(punishment, "mute")
                      || string.Equals(punishment, "kick")
                      || string.Equals(punishment, "ban")
                      || string.Equals(punishment, "softban")
                      || string.Equals(punishment, "pruneban")))
                {
                    await ReplyErrorAsync(Localization.AdministrationWarningInvalidPunishment);
                    return;
                }
                
                guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                guildDb.PunishmentWarningsRequired = number;
                guildDb.WarningPunishment = punishment;

                await DbContext.SaveChangesAsync();
                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Description = GetText(Localization.AdministrationWarningPunishmentSet)
                    }.AddField(GetText(Localization.AdministrationWarnings), number, true)
                    .AddField(GetText(Localization.AdministrationPunishment), punishment.Titleize(), true);

                await ReplyAsync(embed);
            }
            
            private PermissionRequired CheckRequiredPermissions(CachedMember member, GuildsEntity guildDb)
            {
                if (member.Id == member.Guild.OwnerId || member.Permissions.Administrator)
                    return PermissionRequired.NoPermission;

                var warnPunishment = guildDb?.WarningPunishment;
                if (string.IsNullOrEmpty(warnPunishment))
                    return member.Permissions.MuteMembers || member.Permissions.KickMembers || member.Permissions.BanMembers
                        ? PermissionRequired.NoPermission
                        : PermissionRequired.MuteKickBan;

                if (string.Equals(warnPunishment, "mute", StringComparison.InvariantCultureIgnoreCase))
                    return member.Permissions.MuteMembers ? PermissionRequired.NoPermission : PermissionRequired.Mute;

                if (string.Equals(warnPunishment, "kick", StringComparison.InvariantCultureIgnoreCase))
                    return member.Permissions.KickMembers ? PermissionRequired.NoPermission : PermissionRequired.Kick;

                if (string.Equals(warnPunishment, "ban", StringComparison.InvariantCultureIgnoreCase))
                    return member.Permissions.BanMembers ? PermissionRequired.NoPermission : PermissionRequired.Ban;

                if (string.Equals(warnPunishment, "softban", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (member.Id == member.Guild.CurrentMember.Id)
                    {
                        return member.Permissions.KickMembers && member.Permissions.BanMembers
                            ? PermissionRequired.NoPermission
                            : PermissionRequired.KickBan;
                    }

                    return member.Permissions.KickMembers ? PermissionRequired.NoPermission : PermissionRequired.Kick;
                }

                if (string.Equals(warnPunishment, "pruneban", StringComparison.InvariantCultureIgnoreCase))
                    return member.Permissions.BanMembers ? PermissionRequired.NoPermission : PermissionRequired.Ban;

                return default;
            }
            
            private async Task SendMissingPermissionsAsync(string userType, PermissionRequired permissions, GuildsEntity guildDb)
            {
                switch (permissions)
                {
                    case PermissionRequired.MuteKickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                            .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                        await ReplyErrorAsync(Localization.AdministrationWarningUserTypeNoPermissionsDefault(userType), permsHumanized);
                        return;
                    }
                    case PermissionRequired.KickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                            .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                        await ReplyErrorAsync(Localization.AdministrationWarningUserTypeNoPermissionsPunishment(userType), permsHumanized);
                        return;
                    }
                }
                
                var punishmentHumanized = guildDb.WarningPunishment!.ToLower();
                var permHumanized = GetText(Localization.AdministrationPermission(permissions.Humanize().ToLower()));
                await ReplyErrorAsync(Localization.AdministrationWarningUserTypeNoPermissionsPunishment(userType), punishmentHumanized, permHumanized);
            }
            
            private async Task ApplyWarnPunishmentAsync(CachedMember member, PunishmentMethod punishment, GuildsEntity guildDb)
            {
                switch (punishment)
                {
                    case PunishmentMethod.Mute:
                        await _muteService.MuteUserAsync(Context.Channel, Context.CurrentMember!, member, GetText(Localization.AdministrationWarningMute));
                        break;
                    case PunishmentMethod.Kick:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationUserKicked, Localization.AdministrationKickedFrom, GetText(Localization.AdministrationWarningKick));
                        await member.KickAsync();
                        break;
                    case PunishmentMethod.Ban:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationUserBanned, Localization.AdministrationBannedFrom, GetText(Localization.AdministrationWarningBan));
                        await member.BanAsync();
                        break;
                    case PunishmentMethod.SoftBan:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationUserSoftBanned, Localization.AdministrationKickedFrom, GetText(Localization.AdministrationWarningKick));
                        await member.BanAsync(messageDeleteDays: 7);
                        await member.UnbanAsync();
                        break;
                    case PunishmentMethod.PruneBan:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationUserBanned, Localization.AdministrationBannedFrom, GetText(Localization.AdministrationWarningBan));
                        await member.BanAsync(messageDeleteDays: 7);
                        break;
                }
            }
            
            private async Task SendMessageAsync(CachedMember member, GuildsEntity guildDb, string moderationType, string fromWhere, string? reason)
            {
                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ErrorColor,
                        Title = GetText(moderationType),
                        ThumbnailUrl = member.GetAvatarUrl()
                    }.AddField(GetText(Localization.CommonUser), member, true)
                    .AddField(GetText(Localization.CommonId), member.Id.ToString(), true)
                    .AddField(GetText(Localization.AdministrationModerator), Context.User, true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText(Localization.CommonReason), reason, true);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild!.GetTextChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentMember!.GetPermissionsFor(modLogChannel);
                    if (preconditions.ViewChannel && preconditions.SendMessages)
                        channel = modLogChannel;
                }

                if (channel.Id != Context.Channel.Id)
                    await Context.Message.AddReactionAsync(new LocalEmoji("✅"));

                await channel.SendMessageAsync(embed);

                var reasonEmbed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ErrorColor,
                    Description = GetText(fromWhere, Context.Guild.Name)
                };

                if (!string.IsNullOrEmpty(reason))
                    reasonEmbed.AddField(GetText(Localization.CommonReason), reason, true);

                try
                {
                    if (!member.IsBot)
                        await member.SendMessageAsync(reasonEmbed);
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