using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Warnings")]
        public class WarningsSubmodule : RiasModule
        {
            private readonly MuteService _muteService;
            
            public WarningsSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _muteService = serviceProvider.GetRequiredService<MuteService>();
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

            [Command("warn")]
            [Context(ContextType.Guild)]
            public async Task WarnAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });

                var userRequiredPermissions = CheckRequiredPermissions((DiscordMember) Context.User, guildDb);
                if (userRequiredPermissions != PermissionRequired.NoPermission)
                {
                    await SendMissingPermissionsAsync("member", userRequiredPermissions, guildDb);
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

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotWarnOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAbove);
                    return;
                }

                var userWarnings = await DbContext.GetListAsync<WarningEntity>(x => x.GuildId == Context.Guild!.Id && x.UserId == member.Id);
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
                    await ReplyErrorAsync(Localization.AdministrationMemberWarningsLimit, member.DisplayName, 10);
                    return;
                }

                Enum.TryParse<PunishmentMethod>(guildDb.WarningPunishment, true, out var punishment);
                
                if (applyPunishment && punishment != PunishmentMethod.NoPunishment)
                {
                    await ApplyWarnPunishmentAsync(member, punishment, guildDb);
                    return;
                }
                
                await DbContext.AddAsync(new WarningEntity
                {
                    GuildId = member.Guild.Id,
                    UserId = member.Id,
                    ModeratorId = Context.User.Id,
                    Reason = reason
                });
                
                await DbContext.SaveChangesAsync();
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.Yellow,
                        Title = GetText(Localization.AdministrationMemberWarned)
                    }.WithThumbnail(member.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.CommonMember), member.FullName(), true)
                    .AddField(GetText(Localization.CommonId), member.Id.ToString(), true)
                    .AddField(GetText(Localization.AdministrationWarningNumber), (warnsCount + 1).ToString(), true)
                    .AddField(GetText(Localization.AdministrationModerator), Context.User.FullName(), true);
                
                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText(Localization.CommonReason), reason, true);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild!.GetChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentMember!.PermissionsIn(modLogChannel);
                    if (preconditions.HasPermission(Permissions.AccessChannels) && preconditions.HasPermission(Permissions.SendMessages))
                    {
                        await ReplyConfirmationAsync(Localization.AdministrationMemberWasWarned, member.FullName(), modLogChannel.Mention);
                        channel = modLogChannel;
                    }
                }

                await channel.SendMessageAsync(embed);
            }

            [Command("warnings", "warninglist", "warnlist")]
            [Context(ContextType.Guild)]
            [CheckDownloadedMembers]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task WarningsAsync()
            {
                var warnings = (await DbContext.GetListAsync<WarningEntity>(x => x.GuildId == Context.Guild!.Id))
                    .GroupBy(x => x.UserId)
                    .Where(x => Context.Guild!.Members.ContainsKey(x.First().UserId))
                    .Select(x => Context.Guild!.Members[x.First().UserId])
                    .OrderBy(u => u.Username)
                    .ToList();

                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationNoWarnedMembers);
                    return;
                }
                
                await SendPaginatedMessageAsync(warnings, 15, (items, index) => new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationWarnedMembers),
                    Description = string.Join("\n", items.Select(x => $"{++index}. {x.FullName()} • `{x.Id}`")),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = GetText(Localization.AdministrationWarningListFooter, Context.Prefix)
                    }
                });
            }

            [Command("warnings", "warninglist", "warnlist")]
            [Context(ContextType.Guild)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task WarningsAsync([Remainder] DiscordMember member)
            {
                var warningsDb = await DbContext.GetListAsync<WarningEntity>(x => x.GuildId == member.Guild.Id && x.UserId == member.Id);
                var moderators = (await Task.WhenAll(warningsDb.Select(async x => await RiasBot.GetMemberAsync(Context.Guild!, x.ModeratorId)))).ToList();
                
                var warnings = warningsDb.Select((x, i) => new
                {
                    x.DateAdded,
                    Moderator = moderators[i]?.Mention ?? x.ModeratorId.ToString(),
                    x.Reason
                }).ToList();

                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberNoWarnings, member.FullName());
                    return;
                }
                
                await SendPaginatedMessageAsync(warnings, 5, (items, index) => new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationMemberWarnings, member.FullName()),
                    Description = string.Join("\n", items.Select(x => $"{++index}. {x.Reason ?? "-"}\n" +
                                                                      $"{GetText(Localization.AdministrationModerator)}: {x.Moderator}\n" +
                                                                      $"{GetText(Localization.CommonDate)}: `{x.DateAdded:yyyy-MM-dd HH:mm:ss}`"))
                });
            }

            [Command("clearwarning", "clearwarn")]
            [Context(ContextType.Guild)]
            [Priority(1)]
            public async Task ClearWarningAsync(DiscordMember member, int warningIndex)
            {
                if (--warningIndex < 0)
                    return;

                var permissions = ((DiscordMember) Context.User).GetPermissions();
                if (!(permissions.HasPermission(Permissions.Administrator)
                      || permissions.HasPermission(Permissions.MuteMembers)
                      || permissions.HasPermission(Permissions.KickMembers)
                      || permissions.HasPermission(Permissions.BanMembers)))
                {
                    var permsHumanized = PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                        .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                    await ReplyErrorAsync(Localization.AdministrationClearWarningMemberNoPermissions, permsHumanized);
                    return;
                }

                var warnings = await DbContext.GetListAsync<WarningEntity>(x => x.GuildId == member.Guild.Id && x.UserId == member.Id);
                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberNoWarnings, member.FullName());
                    return;
                }

                if (warningIndex >= warnings.Count)
                {
                    await ReplyErrorAsync(Localization.AdministrationClearWarningIndexAbove, member.FullName());
                    return;
                }

                var warning = warnings[warningIndex];
                if (Context.User.Id != warning.ModeratorId && !permissions.HasPermission(Permissions.Administrator))
                {
                    await ReplyErrorAsync(Localization.AdministrationClearWarningNotMemberWarning, member.FullName());
                    return;
                }

                DbContext.Remove(warning);
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.AdministrationWarningCleared, member.FullName());
            }

            [Command("clearwarning", "clearwarn")]
            [Context(ContextType.Guild)]
            [Priority(0)]
            public async Task ClearWarningAsync(DiscordMember member, string all)
            {
                if (!string.Equals(all, "all", StringComparison.InvariantCultureIgnoreCase))
                    return;

                var permissions = ((DiscordMember) Context.User).GetPermissions();
                if (!(permissions.HasPermission(Permissions.Administrator)
                      || permissions.HasPermission(Permissions.MuteMembers)
                      || permissions.HasPermission(Permissions.KickMembers)
                      || permissions.HasPermission(Permissions.BanMembers)))
                {
                    var permsHumanized = PermissionRequired.MuteKickBan.Humanize()
                        .Split(" ")
                        .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                        .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                    await ReplyErrorAsync(Localization.AdministrationClearWarningMemberNoPermissions, permsHumanized);
                    return;
                }

                var warnings = await DbContext.GetListAsync<WarningEntity>(x => x.GuildId == member.Guild.Id && x.UserId == member.Id);
                if (warnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberNoWarnings, member.FullName());
                    return;
                }

                var moderatorWarnings = permissions.HasPermission(Permissions.Administrator) ? warnings : warnings.Where(x => x.ModeratorId == Context.User.Id).ToList();
                if (moderatorWarnings.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationClearWarningNotModWarnings, member.FullName());
                    return;
                }
                
                DbContext.RemoveRange(moderatorWarnings);
                await DbContext.SaveChangesAsync();
                if (permissions.HasPermission(Permissions.Administrator) || moderatorWarnings.Count == warnings.Count)
                {
                    await ReplyConfirmationAsync(Localization.AdministrationAllWarningsCleared, member.FullName());
                }
                else
                {
                    var warningsString = moderatorWarnings.Count > 1 ? GetText(Localization.AdministrationWarnings) : GetText(Localization.AdministrationWarning);
                    await ReplyConfirmationAsync(Localization.AdministrationWarningsCleared, moderatorWarnings.Count, warningsString.ToLowerInvariant(), member.FullName());
                }
            }

            [Command("warningpunishment", "warnpunishment")]
            [Context(ContextType.Guild)]
            public async Task WarningPunishmentAsync()
            {
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
                if (guildDb.PunishmentWarningsRequired == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationNoWarningPunishment);
                    return;
                }

                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Description = GetText(Localization.AdministrationWarningPunishment)
                    }.AddField(GetText(Localization.AdministrationWarnings), guildDb.PunishmentWarningsRequired.ToString(), true)
                    .AddField(GetText(Localization.AdministrationPunishment), guildDb.WarningPunishment.Titleize(), true);

                await ReplyAsync(embed);
            }

            [Command("setwarningpunishment", "setwarnpunishment")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.Administrator)]
            public async Task SetWarningPunishmentAsync(int number, string? punishment = null)
            {
                if (number < 0)
                    return;

                if (number > 10)
                {
                    await ReplyErrorAsync(Localization.AdministrationWarningsLimit, 10);
                    return;
                }

                GuildEntity guildDb;
                if (number == 0)
                {
                    guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
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
                
                guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
                guildDb.PunishmentWarningsRequired = number;
                guildDb.WarningPunishment = punishment;

                await DbContext.SaveChangesAsync();
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Description = GetText(Localization.AdministrationWarningPunishmentSet)
                    }.AddField(GetText(Localization.AdministrationWarnings), number.ToString(), true)
                    .AddField(GetText(Localization.AdministrationPunishment), punishment.Titleize(), true);

                await ReplyAsync(embed);
            }
            
            private PermissionRequired CheckRequiredPermissions(DiscordMember member, GuildEntity? guildDb)
            {
                var permissions = member.GetPermissions();
                if (member.Id == member.Guild.Owner.Id || permissions.HasPermission(Permissions.Administrator))
                    return PermissionRequired.NoPermission;

                var warnPunishment = guildDb?.WarningPunishment;
                if (string.IsNullOrEmpty(warnPunishment))
                {
                    return permissions.HasPermission(Permissions.MuteMembers)
                           || permissions.HasPermission(Permissions.KickMembers)
                           || permissions.HasPermission(Permissions.BanMembers)
                        ? PermissionRequired.NoPermission
                        : PermissionRequired.MuteKickBan;
                }

                if (string.Equals(warnPunishment, "mute", StringComparison.InvariantCultureIgnoreCase))
                    return permissions.HasPermission(Permissions.MuteMembers) ? PermissionRequired.NoPermission : PermissionRequired.Mute;

                if (string.Equals(warnPunishment, "kick", StringComparison.InvariantCultureIgnoreCase))
                    return permissions.HasPermission(Permissions.KickMembers) ? PermissionRequired.NoPermission : PermissionRequired.Kick;

                if (string.Equals(warnPunishment, "ban", StringComparison.InvariantCultureIgnoreCase))
                    return permissions.HasPermission(Permissions.BanMembers) ? PermissionRequired.NoPermission : PermissionRequired.Ban;

                if (string.Equals(warnPunishment, "softban", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (member.Id == member.Guild.CurrentMember.Id)
                    {
                        return permissions.HasPermission(Permissions.KickMembers) && permissions.HasPermission(Permissions.BanMembers)
                            ? PermissionRequired.NoPermission
                            : PermissionRequired.KickBan;
                    }

                    return permissions.HasPermission(Permissions.KickMembers) ? PermissionRequired.NoPermission : PermissionRequired.Kick;
                }

                if (string.Equals(warnPunishment, "pruneban", StringComparison.InvariantCultureIgnoreCase))
                    return permissions.HasPermission(Permissions.BanMembers) ? PermissionRequired.NoPermission : PermissionRequired.Ban;

                return default;
            }
            
            private async Task SendMissingPermissionsAsync(string memberType, PermissionRequired permissions, GuildEntity guildDb)
            {
                switch (permissions)
                {
                    case PermissionRequired.MuteKickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                            .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                        await ReplyErrorAsync(Localization.AdministrationWarningMemberTypeNoPermissionsDefault(memberType), permsHumanized);
                        return;
                    }
                    
                    case PermissionRequired.KickBan:
                    {
                        var permsHumanized = permissions.Humanize()
                            .Split(" ")
                            .Select(x => GetText(Localization.AdministrationPermission(x.ToLower())))
                            .Humanize(GetText(Localization.CommonOr).ToLowerInvariant());
                        await ReplyErrorAsync(Localization.AdministrationWarningMemberTypeNoPermissionsPunishment(memberType), permsHumanized);
                        return;
                    }
                }
                
                var punishmentHumanized = guildDb.WarningPunishment!.ToLower();
                var permHumanized = GetText(Localization.AdministrationPermission(permissions.Humanize().ToLower()));
                await ReplyErrorAsync(Localization.AdministrationWarningMemberTypeNoPermissionsPunishment(memberType), punishmentHumanized, permHumanized);
            }
            
            private async Task ApplyWarnPunishmentAsync(DiscordMember member, PunishmentMethod punishment, GuildEntity guildDb)
            {
                switch (punishment)
                {
                    case PunishmentMethod.Mute:
                        await _muteService.MuteUserAsync(Context.Channel, Context.CurrentMember!, member, GetText(Localization.AdministrationWarningMute), sentByWarning: true);
                        break;
                    case PunishmentMethod.Kick:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationMemberKicked, Localization.AdministrationKickedFrom, GetText(Localization.AdministrationWarningKick));
                        await member.RemoveAsync();
                        break;
                    case PunishmentMethod.Ban:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationMemberBanned, Localization.AdministrationBannedFrom, GetText(Localization.AdministrationWarningBan));
                        await member.BanAsync();
                        break;
                    case PunishmentMethod.SoftBan:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationMemberSoftBanned, Localization.AdministrationKickedFrom, GetText(Localization.AdministrationWarningKick));
                        await member.BanAsync(7);
                        await member.UnbanAsync();
                        break;
                    case PunishmentMethod.PruneBan:
                        await SendMessageAsync(member, guildDb, Localization.AdministrationMemberBanned, Localization.AdministrationBannedFrom, GetText(Localization.AdministrationWarningBan));
                        await member.BanAsync(7);
                        break;
                }
            }
            
            private async Task SendMessageAsync(DiscordMember member, GuildEntity guildDb, string moderationType, string fromWhere, string? reason)
            {
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ErrorColor,
                        Title = GetText(moderationType)
                    }.WithThumbnail(member.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.CommonMember), member.FullName(), true)
                    .AddField(GetText(Localization.CommonId), member.Id.ToString(), true)
                    .AddField(GetText(Localization.AdministrationModerator), Context.User.FullName(), true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText(Localization.CommonReason), reason, true);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild!.GetChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentMember!.PermissionsIn(modLogChannel);
                    if (preconditions.HasPermission(Permissions.AccessChannels) && preconditions.HasPermission(Permissions.SendMessages))
                        channel = modLogChannel;
                }

                if (channel.Id != Context.Channel.Id)
                    await ReplyConfirmationAsync(Localization.AdministrationMemberWasWarned, member.FullName(), channel.Mention);

                await channel.SendMessageAsync(embed);

                var reasonEmbed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ErrorColor,
                    Description = GetText(fromWhere, Context.Guild.Name)
                };

                if (!string.IsNullOrEmpty(reason))
                    reasonEmbed.AddField(GetText(Localization.CommonReason), reason, true);

                try
                {
                    if (!member.IsBot)
                        await member.SendMessageAsync(embed: reasonEmbed);
                }
                catch
                {
                    // the user blocked the messages from the guild users
                }
            }
        }
    }
}