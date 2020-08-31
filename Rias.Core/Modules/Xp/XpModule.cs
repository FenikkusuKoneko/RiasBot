using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
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
        public async Task XpAsync(DiscordMember? member = null)
        {
            member ??= (DiscordMember) Context.User;
            await Context.Channel.TriggerTypingAsync();

            var currentMember = Context.Guild!.CurrentMember;
            if (!currentMember.GetPermissions().HasPermission(Permissions.AttachFiles))
            {
                await ReplyErrorAsync(Localization.XpNoAttachFilesPermission);
                return;
            }

            if (!currentMember.PermissionsIn(Context.Channel).HasPermission(Permissions.AttachFiles))
            {
                await ReplyErrorAsync(Localization.XpNoAttachFilesChannelPermission);
                return;
            }

            await using var xpImage = await Service.GenerateXpImageAsync(member);
            await Context.Channel.SendFileAsync($"{member.Id}_xp.png", xpImage);
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

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpLeaderboard)
            };

            var description = new StringBuilder();
            var index = page * 15;
            foreach (var userDb in xpLeaderboard)
            {
                var member = RiasBot.Members.TryGetValue(userDb.UserId, out var m)
                    ? m
                    : await RiasBot.Client.ShardClients[0].GetUserAsync(userDb.UserId);
                
                description.Append($"{++index}. **{member.FullName()}**: " +
                                   $"`{GetText(Localization.XpLevelX, RiasUtilities.XpToLevel(userDb.Xp, XpService.XpThreshold))} " +
                                   $"({userDb.Xp} {GetText(Localization.XpXp).ToLowerInvariant()})`\n");
            }

            embed.WithDescription(description.ToString());
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
                .Where(x => Context.Guild!.Members.ContainsKey(x.UserId))
                .Skip(page * 15)
                .Take(15)
                .ToList();
            
            if (xpLeaderboard.Count == 0)
            {
                await ReplyErrorAsync(Localization.XpGuildLeaderboardEmpty);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpGuildLeaderboard)
            };

            var description = new StringBuilder();
            var index = page * 15;
            foreach (var userDb in xpLeaderboard)
            {
                var member = await Context.Guild!.GetMemberAsync(userDb.UserId);
                if (member is null)
                    continue;

                description.Append($"{++index}. {member.Mention}: " +
                                   $"`{GetText(Localization.XpLevelX, RiasUtilities.XpToLevel(userDb.Xp, XpService.XpThreshold))} " +
                                   $"({userDb.Xp} {GetText(Localization.XpXp).ToLowerInvariant()})`\n");
            }

            embed.WithDescription(description.ToString());
            await ReplyAsync(embed);
        }
        
        [Command("xpnotification"), Context(ContextType.Guild),
         UserPermission(Permissions.Administrator), BotPermission(Permissions.ManageWebhooks),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task XpNotificationAsync([Channel(ChannelType.Text), Remainder] DiscordChannel? channel = null)
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
                await using var stream = await _httpClient.GetStreamAsync(currentMember.GetAvatarUrl(ImageFormat.Auto));
                await using var webhookAvatar = new MemoryStream();
                await stream.CopyToAsync(webhookAvatar);
                webhookAvatar.Position = 0;
                
                var webhook = guildDb.XpWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.XpWebhookId) : null;
                if (webhook != null && webhook.ChannelId != channel.Id)
                    await webhook.ModifyAsync(channelId: channel.Id);
                else
                    webhook = await channel.CreateWebhookAsync(currentMember.Username, webhookAvatar);

                guildDb.XpNotification = true;
                guildDb.XpWebhookId = webhook.Id;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.XpNotificationEnabledChannel, channel.Mention);
            }
        }

        [Command("xpmessage"), Context(ContextType.Guild),
         UserPermission(Permissions.Administrator)]
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

            var xpMessage = XpService.ReplacePlaceholders((DiscordMember) Context.User, null, 1, message);
            var xpMessageParsed = RiasUtilities.TryParseMessage(xpMessage, out var customMessage);

            if (xpMessageParsed && string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null)
            {
                await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
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
                    var webhookChannel = Context.Guild!.GetChannel(webhook.ChannelId);
                    reply += GetText(Localization.XpNotificationSetChannel, webhookChannel.Mention);
                }
            }
            else if (guildDb.XpNotification)
                reply += GetText(Localization.XpNotificationSet);
            else
                reply += GetText(Localization.XpNotificationNotEnabled);

            if (xpMessageParsed)
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{customMessage.Content}";
                await Context.Channel.SendMessageAsync(reply, embed: customMessage.Embed);
            }
            else
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{xpMessage}";
                await Context.Channel.SendMessageAsync(reply);
            }
        }
        
        [Command("xpmessagereward"), Context(ContextType.Guild),
         UserPermission(Permissions.Administrator)]
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

            var xpRoles = await DbContext.GetOrderedListAsync<GuildXpRolesEntity, int>(x => x.GuildId == Context.Guild!.Id, x => x.Level);

            var level = xpRoles.Count != 0 ? xpRoles[0].Level : 0;
            var role = xpRoles.Count != 0 ? Context.Guild!.GetRole(xpRoles[0].RoleId) : null;

            var xpMessageReward = XpService.ReplacePlaceholders((DiscordMember) Context.User, role, level, message);
            var xpMessageRewardParsed = RiasUtilities.TryParseMessage(xpMessageReward, out var customMessage);
            if (xpMessageRewardParsed && string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null)
            {
                await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
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
                    var webhookChannel = Context.Guild!.GetChannel(webhook.ChannelId);
                    reply += GetText(Localization.XpNotificationSetChannel, webhookChannel.Mention);
                }
            }
            else if (guildDb.XpNotification)
                reply += GetText(Localization.XpNotificationSet);
            else
                reply += GetText(Localization.XpNotificationNotEnabled);

            if (xpMessageRewardParsed)
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{customMessage.Content}";
                await Context.Channel.SendMessageAsync(reply, embed: customMessage.Embed);
            }
            else
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{xpMessageReward}";
                await Context.Channel.SendMessageAsync(reply);
            }
        }
        
        [Command("leveluprolereward"), Context(ContextType.Guild),
         UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles)]
        public async Task LevelUpRoleRewardAsync(int level, [Remainder] DiscordRole? role = null)
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
            
            if (role.Id == Context.Guild!.EveryoneRole.Id) return;
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
         BotPermission(Permissions.ManageRoles)]
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

            await SendPaginatedMessageAsync(levelRoles, 15, (items, indes) => new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpLevelUpRoleRewardList),
                Description = string.Join('\n', items.Select(lr => $"{GetText(Localization.XpLevelX, lr.Level)}: {Context.Guild!.GetRole(lr.RoleId).Mention}"))
            });
        }
        
        [Command("resetserverxp"), Context(ContextType.Guild),
         UserPermission(Permissions.Administrator)]
        public async Task ResetGuildXpAsync()
        {
            await ReplyConfirmationAsync(Localization.XpResetGuildXpConfirmation);
            
            var messageReceived = await NextMessageAsync();
            if (!string.Equals(messageReceived?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
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