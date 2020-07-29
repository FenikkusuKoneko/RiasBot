using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Xp
{
    [Name("Xp")]
    public class XpModule : RiasModule<XpService>
    {
        private readonly HttpClient _httpClient;
        
        public XpModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }
        
        [Command("xp"), Context(ContextType.Guild),
         Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task XpAsync(CachedMember? member = null)
        {
            member ??= (CachedMember) Context.User;
            using var _ = Context.Channel.Typing();

            var currentMember = Context.Guild!.CurrentMember;
            if (!currentMember.Permissions.AttachFiles)
            {
                await ReplyErrorAsync(Localization.XpNoAttachFilesPermission);
                return;
            }

            if (!currentMember.GetPermissionsFor((CachedTextChannel) Context.Channel).AttachFiles)
            {
                await ReplyErrorAsync(Localization.XpNoAttachFilesChannelPermission);
                return;
            }

            await using var xpImage = await Service.GenerateXpImageAsync(member);
            await Context.Channel.SendMessageAsync(new[] {new LocalAttachment(xpImage, $"{member.Id}_xp.png")});
        }
        
        [Command("globalxpleaderboard"),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task GlobalXpLeaderboardAsync(int page = 1)
        {
            page--;
            if (page < 0)
                page = 0;

            var xpLeaderboard = await DbContext.GetOrderedListAsync<UsersEntity, int>(x => x.Xp, true, (page * 15)..((page + 1) * 15));
            if (xpLeaderboard.Count == 0)
            {
                await ReplyErrorAsync(Localization.XpLeaderboardEmpty);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpLeaderboard)
            };

            var index = page * 15;
            foreach (var userDb in xpLeaderboard)
            {
                var user = (IUser) RiasBot.GetUser(userDb.UserId) ?? await RiasBot.GetUserAsync(userDb.UserId);
                embed.AddField($"{++index}. {user}",
                    $"{GetText(Localization.XpLevelX, RiasUtilities.XpToLevel(userDb.Xp, XpService.XpThreshold))} | {GetText(Localization.XpXp)} {userDb.Xp}",
                    true);
            }

            await ReplyAsync(embed);
        }
        
        [Command("xpleaderboard"), Context(ContextType.Guild),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task XpLeaderboardAsync(int page = 1)
        {
            page--;
            if (page < 0)
                page = 0;
            
            var xpLeaderboard = (await DbContext.GetOrderedListAsync<GuildUsersEntity, int>(x => x.GuildId == Context.Guild!.Id, y => y.Xp, true))
                .Where(x => Context.Guild!.GetMember(x.UserId) != null)
                .Skip(page * 15)
                .Take(15)
                .ToList();
            
            if (xpLeaderboard.Count == 0)
            {
                await ReplyErrorAsync(Localization.XpGuildLeaderboardEmpty);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpGuildLeaderboard)
            };

            var index = page * 15;
            foreach (var userDb in xpLeaderboard)
            {
                embed.AddField($"{++index}. {Context.Guild!.GetMember(userDb.UserId)}",
                    $"{GetText(Localization.XpLevelX, RiasUtilities.XpToLevel(userDb.Xp, XpService.XpThreshold))} | {GetText(Localization.XpXp)} {userDb.Xp}",
                    true);
            }

            await ReplyAsync(embed);
        }
        
        [Command("xpnotification"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator), BotPermission(Permission.ManageWebhooks),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task XpNotificationAsync(CachedTextChannel? channel = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            
            if (channel is null)
            {
                guildDb.XpNotification = !guildDb.XpNotification;
                if (guildDb.XpNotification)
                {
                    await DbContext.SaveChangesAsync();
                    await ReplyConfirmationAsync(Localization.XpNotificationEnabled);
                }
                else
                {
                    var webhook = guildDb.XpWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.XpWebhookId) : null;
                    if (webhook != null)
                        await webhook.DeleteAsync();

                    guildDb.XpWebhookId = 0;
                    await DbContext.SaveChangesAsync();
                    await ReplyConfirmationAsync(Localization.XpNotificationDisabled);
                }
            }
            else
            {
                var currentMember = Context.CurrentMember!;
                await using var stream = await _httpClient.GetStreamAsync(currentMember.GetAvatarUrl());
                await using var webhookAvatar = new MemoryStream();
                await stream.CopyToAsync(webhookAvatar);
                webhookAvatar.Position = 0;
                
                var webhook = guildDb.XpWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.XpWebhookId) : null;
                if (webhook != null && webhook.ChannelId != channel.Id)
                    await webhook.ModifyAsync(x => x.ChannelId = channel.Id);
                else
                    webhook = await channel.CreateWebhookAsync(currentMember.Name, webhookAvatar);

                guildDb.XpNotification = true;
                guildDb.XpWebhookId = webhook.Id;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.XpNotificationEnabledChannel, channel.Mention);
            }
        }

        [Command("xpmessage"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task XpMessageAsync([Remainder] string? message = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            if (string.IsNullOrEmpty(message))
            {
                guildDb.XpLevelUpMessage = null;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.XpNotificationMessageRemoved);
                return;
            }
            
            if (message.Length > 1500)
            {
                await ReplyErrorAsync(Localization.XpNotificationMessageLengthLimit, 1500);
                return;
            }

            guildDb.XpLevelUpMessage = message;
            await DbContext.SaveChangesAsync();
            
            var reply = $"{GetText(Localization.XpNotificationMessageSet)}\n";
            if (guildDb.XpNotification && guildDb.XpWebhookId > 0)
            {
                var webhook = await Context.Guild!.GetWebhookAsync(guildDb.XpWebhookId);
                if (webhook is null)
                {
                    reply += GetText(Localization.XpNotificationNotEnabled);
                }
                else
                {
                    var webhookChannel = Context.Guild!.GetTextChannel(webhook.ChannelId);
                    reply += GetText(Localization.XpNotificationSetChannel, webhookChannel.Mention);
                }
            }
            else if (guildDb.XpNotification)
                reply += GetText(Localization.XpNotificationSet);
            else
                reply += GetText(Localization.XpNotificationNotEnabled);

            if (RiasUtilities.TryParseEmbed(message, out var embed))
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}";
                await Context.Channel.SendMessageAsync(reply, embed: embed.Build());
            }
            else
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{message}";
                await Context.Channel.SendMessageAsync(reply);
            }
        }
        
        [Command("xpmessagereward"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task XpMessageRewardAsync([Remainder] string? message = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            if (string.IsNullOrEmpty(message))
            {
                guildDb.XpLevelUpRoleRewardMessage = null;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.XpNotificationRewardMessageRemoved);
                return;
            }
            
            if (message.Length > 1500)
            {
                await ReplyErrorAsync(Localization.XpNotificationMessageLengthLimit, 1500);
                return;
            }

            guildDb.XpLevelUpRoleRewardMessage = message;
            await DbContext.SaveChangesAsync();
            
            var reply = $"{GetText(Localization.XpNotificationRewardMessageSet)}\n";
            if (guildDb.XpNotification && guildDb.XpWebhookId > 0)
            {
                var webhook = await Context.Guild!.GetWebhookAsync(guildDb.XpWebhookId);
                if (webhook is null)
                {
                    reply += GetText(Localization.XpNotificationNotEnabled);
                }
                else
                {
                    var webhookChannel = Context.Guild!.GetTextChannel(webhook.ChannelId);
                    reply += GetText(Localization.XpNotificationSetChannel, webhookChannel.Mention);
                }
            }
            else if (guildDb.XpNotification)
                reply += GetText(Localization.XpNotificationSet);
            else
                reply += GetText(Localization.XpNotificationNotEnabled);

            if (RiasUtilities.TryParseEmbed(message, out var embed))
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}";
                await Context.Channel.SendMessageAsync(reply, embed: embed.Build());
            }
            else
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{message}";
                await Context.Channel.SendMessageAsync(reply);
            }
        }
        
        [Command("leveluprolereward"), Context(ContextType.Guild),
         UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles)]
        public async Task LevelUpRoleRewardAsync(int level, CachedRole? role = null)
        {
            if (level < 1)
            {
                await ReplyErrorAsync(Localization.XpLevelUpRoleRewardLimit);
                return;
            }
            
            if (role is null)
            {
                var xpRoleLevelDb = await DbContext.GuildXpRoles.FirstOrDefaultAsync(x => x.GuildId == Context.Guild!.Id && x.Level == level);
                if (xpRoleLevelDb != null)
                {
                    DbContext.Remove(xpRoleLevelDb);
                    await DbContext.SaveChangesAsync();
                }
                
                await ReplyConfirmationAsync(Localization.XpLevelUpRoleRewardRemoved, level);
                return;
            }
            
            if (role.IsDefault) return;
            if (role.IsManaged)
            {
                await ReplyErrorAsync(Localization.XpLevelUpRoleRewardNotSet, role.Name);
                return;
            }

            if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
            {
                await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                return;
            }

            var xpRoleDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id && (x.Level == level || x.RoleId == role.Id),
                () => new GuildXpRolesEntity {GuildId = Context.Guild!.Id});
            xpRoleDb.Level = level;
            xpRoleDb.RoleId = role.Id;
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.XpLevelUpRoleRewardSet, role.Name, level);
        }
        
        [Command("leveluprolerewardlist"), Context(ContextType.Guild),
         BotPermission(Permission.ManageRoles)]
        public async Task LevelUpRoleRewardListAsync()
        {
            var levelRoles = await DbContext.GetOrderedListAsync<GuildXpRolesEntity, int>(x => x.GuildId == Context.Guild!.Id, y => y.Level);
            DbContext.RemoveRange(levelRoles.Where(x => Context.Guild!.GetRole(x.RoleId) is null));
            await DbContext.SaveChangesAsync();
            
            if (levelRoles.Count == 0)
            {
                await ReplyErrorAsync(Localization.XpNoLevelUpRoleReward);
                return;
            }

            await SendPaginatedMessageAsync(levelRoles, 15, (items, indes) => new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpLevelUpRoleRewardList),
                Description = string.Join('\n', items.Select(lr => $"{GetText(Localization.XpLevelX, lr.Level)}: {Context.Guild!.GetRole(lr.RoleId)}"))
            });
        }
        
        [Command("resetserverxp"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task ResetGuildXpAsync()
        {
            await ReplyConfirmationAsync(Localization.XpResetGuildXpConfirmation);
            
            var messageReceived = await NextMessageAsync();
            if (!string.Equals(messageReceived?.Message.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync(Localization.XpResetGuildXpCanceled);
                return;
            }
            
            DbContext.RemoveRange(await DbContext.GetListAsync<GuildUsersEntity>(x => x.GuildId == Context.Guild!.Id));
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.XpGuildXpReset);
        }
    }
}