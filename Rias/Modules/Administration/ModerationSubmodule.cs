using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Moderation")]
        public class ModerationSubmodule : RiasModule
        {
            public ModerationSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
            
            [Command("kick", "k")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.KickMembers)]
            [BotPermission(Permissions.KickMembers)]
            public async Task KickAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotKickOwner);
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

                await SendMessageAsync(member, Localization.AdministrationMemberKicked, Localization.AdministrationKickedFrom, Localization.AdministrationMemberWasKicked, reason);
                
                var auditLogsReason = string.IsNullOrEmpty(reason)
                    ? GetText(Localization.AdministrationKickAuditLogs, Context.User.FullName())
                    : GetText(Localization.AdministrationKickAuditLogsReason, Context.User.FullName(), reason);
                await member.RemoveAsync(auditLogsReason.Truncate(512));
            }
            
            [Command("ban", "b")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.BanMembers)]
            [BotPermission(Permissions.BanMembers)]
            public async Task BanAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotBanOwner);
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

                await SendMessageAsync(member, Localization.AdministrationMemberBanned, Localization.AdministrationBannedFrom, Localization.AdministrationMemberWasBanned, reason);
                
                var auditLogsReason = string.IsNullOrEmpty(reason)
                    ? GetText(Localization.AdministrationBanAuditLogs, Context.User.FullName())
                    : GetText(Localization.AdministrationBanAuditLogsReason, Context.User.FullName(), reason);
                await member.BanAsync(reason: auditLogsReason.Truncate(512));
            }
            
            [Command("ban", "b")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.BanMembers)]
            [BotPermission(Permissions.BanMembers)]
            public async Task BanAsync(ulong userId, [Remainder] string? reason = null)
            {
                if (userId == Context.User.Id)
                    return;

                if (userId == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotBanOwner);
                    return;
                }

                var user = await RiasBot.GetUserAsync(userId);
                if (user is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                    return;
                }
                
                var bans = await Context.Guild!.GetBansAsync();
                if (bans.Any(b => b.User.Id == userId))
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAlreadyBanned, user.FullName());
                    return;
                }
                
                await ReplyConfirmationAsync(Localization.AdministrationUserBanConfirmation, user.FullName());
                
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived.Result?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.AdministrationBanCanceled);
                    return;
                }

                await SendMessageAsync(user, Localization.AdministrationUserBanned, "", Localization.AdministrationMemberWasBanned, reason, false);
                
                var auditLogsReason = string.IsNullOrEmpty(reason)
                    ? GetText(Localization.AdministrationBanAuditLogs, Context.User.FullName())
                    : GetText(Localization.AdministrationBanAuditLogsReason, Context.User.FullName(), reason);
                await Context.Guild.BanMemberAsync(userId, reason: auditLogsReason.Truncate(512));
            }
            
            [Command("softban", "sb")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.KickMembers)]
            [BotPermission(Permissions.KickMembers | Permissions.BanMembers)]
            public async Task SoftBanAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotSoftbanOwner);
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

                await SendMessageAsync(member, Localization.AdministrationMemberSoftBanned, Localization.AdministrationKickedFrom, Localization.AdministrationMemberWasSoftBanned, reason);
                
                var auditLogsReason = string.IsNullOrEmpty(reason)
                    ? GetText(Localization.AdministrationBanAuditLogs, Context.User.FullName())
                    : GetText(Localization.AdministrationBanAuditLogsReason, Context.User.FullName(), reason);
                await member.BanAsync(7, auditLogsReason.Truncate(512));
                await member.UnbanAsync();
            }
            
            [Command("pruneban", "pb")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.BanMembers)]
            [BotPermission(Permissions.BanMembers)]
            public async Task PruneBanAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotPrunebanOwner);
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

                await SendMessageAsync(member, Localization.AdministrationMemberBanned, Localization.AdministrationBannedFrom, Localization.AdministrationMemberWasBanned, reason);
                
                var auditLogsReason = string.IsNullOrEmpty(reason)
                    ? GetText(Localization.AdministrationBanAuditLogs, Context.User.FullName())
                    : GetText(Localization.AdministrationBanAuditLogsReason, Context.User.FullName(), reason);
                await member.BanAsync(7, auditLogsReason.Truncate(512));
            }

            [Command("unban", "ub")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.BanMembers)]
            [BotPermission(Permissions.BanMembers)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task UnbanAsync(string user, [Remainder] string? reason = null)
            {
                var bans = await Context.Guild!.GetBansAsync();
                DiscordUser? bannedUser = null;
                
                if (ulong.TryParse(user, out var userId))
                {
                    bannedUser = bans.FirstOrDefault(b => b.User.Id == userId)?.User;
                }
                else
                {
                    var index = user.LastIndexOf("#", StringComparison.Ordinal);
                    if (index > 0)
                    {
                        var username = user[..index];
                        var discriminator = user[(index + 1)..];
                        if (discriminator.Length == 4 && int.TryParse(discriminator, out _))
                            bannedUser = bans.FirstOrDefault(b => string.Equals(b.User.Discriminator, discriminator)
                                                                  && string.Equals(b.User.Username, username, StringComparison.OrdinalIgnoreCase))?.User;
                    }
                    
                    bannedUser ??= bans.FirstOrDefault(u => string.Equals(u.User.Username, user, StringComparison.OrdinalIgnoreCase))?.User;
                }
                
                if (bannedUser is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationBanNotFound);
                    return;
                }
                
                await ReplyConfirmationAsync(Localization.AdministrationUnbanConfirmation, bannedUser.FullName());
                
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived.Result?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.AdministrationUnbanCanceled);
                    return;
                }
                
                await SendMessageAsync(bannedUser, Localization.AdministrationUserUnbanned, "", Localization.AdministrationUserWasUnbanned, reason, false);
                
                var auditLogsReason = string.IsNullOrEmpty(reason)
                    ? GetText(Localization.AdministrationUnbanAuditLogs, Context.User.FullName())
                    : GetText(Localization.AdministrationUnbanAuditLogsReason, Context.User.FullName(), reason);
                await Context.Guild!.UnbanMemberAsync(bannedUser, auditLogsReason.Truncate(512));
            }
            
            [Command("prune", "purge")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageMessages)]
            [BotPermission(Permissions.ManageMessages)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(2)]
            public async Task PruneAsync(int amount = 100)
            {
                if (amount < 1)
                    return;
                if (amount < 100)
                    amount++;
                else
                    amount = 100;

                var messages = (await Context.Channel.GetMessagesAsync(amount))
                    .Where(m => DateTime.UtcNow.Subtract(m.CreationTimestamp.UtcDateTime).Days < 14)
                    .ToList();

                if (messages.Count != 0)
                    await Context.Channel.DeleteMessagesAsync(messages);
                else
                    await ReplyErrorAsync(Localization.AdministrationPruneLimit);
            }

            [Command("prune", "purge")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageMessages)]
            [BotPermission(Permissions.ManageMessages)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(1)]
            public async Task PruneAsync(int amount, DiscordMember member)
                => await PruneUserMessagesAsync(member, amount);

            [Command("prune", "purge")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageMessages)]
            [BotPermission(Permissions.ManageMessages)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(0)]
            public async Task PruneAsync(DiscordMember member, int amount = 100)
                => await PruneUserMessagesAsync(member, amount);

            private async Task PruneUserMessagesAsync(DiscordMember member, int amount)
            {
                if (amount < 1)
                    return;
                if (member.Id == Context.User.Id && amount < 100)
                    amount++;
                else if (amount > 100)
                    amount = 100;

                var messages = (await Context.Channel.GetMessagesAsync())
                    .Where(m => m.Author.Id == member.Id && DateTime.UtcNow.Subtract(m.CreationTimestamp.UtcDateTime).Days < 14)
                    .Take(amount)
                    .ToList();

                if (messages.Count != 0)
                {
                    if (Context.User.Id != member.Id)
                        messages.Add(Context.Message);
                    await Context.Channel.DeleteMessagesAsync(messages);
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationPruneLimit);
                }
            }

            private async Task SendMessageAsync(DiscordUser user, string moderationType, string fromWhere, string confirmation, string? reason, bool informUser = true)
            {
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ErrorColor,
                        Title = GetText(moderationType)
                    }.WithThumbnail(user.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.CommonMember), user.FullName(), true)
                    .AddField(GetText(Localization.CommonId), user.Id.ToString(), true)
                    .AddField(GetText(Localization.AdministrationModerator), Context.User.FullName(), true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText(Localization.CommonReason), reason.Truncate(1024), true);

                var channel = Context.Channel;
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
                var modLogChannel = Context.Guild!.GetChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentMember!.PermissionsIn(modLogChannel);
                    if (preconditions.HasPermission(Permissions.AccessChannels) && preconditions.HasPermission(Permissions.SendMessages))
                    {
                        await ReplyConfirmationAsync(confirmation, user.FullName(), modLogChannel.Mention);
                        channel = modLogChannel;
                    }
                }

                await channel.SendMessageAsync(embed);

                if (!informUser)
                    return;

                var reasonEmbed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ErrorColor,
                    Description = GetText(fromWhere, Context.Guild.Name)
                };

                if (!string.IsNullOrEmpty(reason))
                    reasonEmbed.AddField(GetText(Localization.CommonReason), reason.Truncate(1024), true);

                try
                {
                    if (!user.IsBot && user is DiscordMember member)
                        await member.SendMessageAsync(reasonEmbed);
                }
                catch
                {
                    // the user blocked the messages from the guild users
                }
            }
        }
    }
}