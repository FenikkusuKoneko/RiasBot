using System;
using System.Collections.Generic;
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
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Xp
{
    [Name("Xp")]
    public class XpModule : RiasModule<XpService>
    {
        private readonly BotService _botService;
        private readonly HttpClient _httpClient;
        
        public XpModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            _botService = serviceProvider.GetRequiredService<BotService>();
        }

        [Command("xp")]
        [Context(ContextType.Guild)]
        [CheckDownloadedMembers]
        [Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task XpAsync([Remainder] DiscordMember? member = null)
        {
            member ??= (DiscordMember) Context.User;
            await Context.Channel.TriggerTypingAsync();

            var serverAttachFilesPerm = Context.Guild!.CurrentMember.GetPermissions().HasPermission(Permissions.AttachFiles);
            var channelAttachFilesPerm = Context.Guild!.CurrentMember.PermissionsIn(Context.Channel).HasPermission(Permissions.AttachFiles);
            if (!serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.XpNoAttachFilesPermission);
                return;
            }

            if (serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.XpNoAttachFilesChannelPermission);
                return;
            }

            await using var xpImage = await Service.GenerateXpImageAsync(member);
            await Context.Channel.SendMessageAsync(new DiscordMessageBuilder().WithFile($"{member.Id}_xp.png", xpImage));
        }

        [Command("globalxpleaderboard", "gxplb")]
        [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task GlobalXpLeaderboardAsync(int page = 1)
        {
            page--;
            if (page < 0)
                page = 0;

            var membersXp = await DbContext.GetOrderedListAsync<UserEntity, int>(x => x.Xp, true);
            var xpLeaderboard = membersXp.Skip(page * 15)
                .Take(15)
                .ToList();
            
            if (xpLeaderboard.Count == 0)
            {
                await ReplyErrorAsync(Localization.XpLeaderboardEmpty);
                return;
            }

            var description = new StringBuilder();
            var index = page * 15;
            foreach (var userDb in xpLeaderboard)
            {
                var user = await RiasBot.GetUserAsync(userDb.UserId);
                description.Append($"{++index}. **{user?.FullName()}**: " +
                                   $"`{GetText(Localization.XpLevelX, RiasUtilities.XpToLevel(userDb.Xp, XpService.XpThreshold))} " +
                                   $"({userDb.Xp} {GetText(Localization.XpXp).ToLowerInvariant()})`\n");
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpLeaderboard),
                Description = description.ToString(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    IconUrl = Context.User.GetAvatarUrl(ImageFormat.Auto),
                    Text = $"{Context.User.FullName()} • #{membersXp.FindIndex(x => x.UserId == Context.User.Id) + 1}"
                }
            };
            
            await ReplyAsync(embed);
        }

        [Command("xpleaderboard", "sxplb", "xplb")]
        [Context(ContextType.Guild)]
        [CheckDownloadedMembers]
        [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task XpLeaderboardAsync(int page = 1)
        {
            page--;
            if (page < 0)
                page = 0;

            var membersXp = (await DbContext.GetOrderedListAsync<MembersEntity, int>(x => x.GuildId == Context.Guild!.Id, y => y.Xp, true))
                .Where(x => Context.Guild!.Members.ContainsKey(x.MemberId))
                .ToList();
            
            var xpLeaderboard = membersXp.Skip(page * 15)
                .Take(15)
                .ToList();
            
            if (xpLeaderboard.Count == 0)
            {
                await ReplyErrorAsync(Localization.XpGuildLeaderboardEmpty);
                return;
            }

            var description = new StringBuilder();
            var index = page * 15;
            foreach (var userDb in xpLeaderboard)
            {
                try
                {
                    var member = await Context.Guild!.GetMemberAsync(userDb.MemberId);
                    description.Append($"{++index}. **{member.FullName()}**: " +
                                       $"`{GetText(Localization.XpLevelX, RiasUtilities.XpToLevel(userDb.Xp, XpService.XpThreshold))} " +
                                       $"({userDb.Xp} {GetText(Localization.XpXp).ToLowerInvariant()})`\n");
                }
                catch
                {
                    // ignored
                }
            }
            
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpGuildLeaderboard),
                Description = description.ToString(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    IconUrl = Context.User.GetAvatarUrl(ImageFormat.Auto),
                    Text = $"{Context.User.FullName()} • #{membersXp.FindIndex(x => x.MemberId == Context.User.Id) + 1}"
                }
            };
            await ReplyAsync(embed);
        }

        [Command("xpnotification", "xpnotify", "xpn")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        [BotPermission(Permissions.ManageWebhooks)]
        [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task XpNotificationAsync([TextChannel, Remainder] DiscordChannel? channel = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            
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

        [Command("xpmessage", "xpm")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task XpMessageAsync([Remainder] string? message = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
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
                await Context.Channel.SendMessageAsync(reply, customMessage.Embed);
            }
            else
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{xpMessage}";
                await Context.Channel.SendMessageAsync(reply);
            }
        }

        [Command("xpmessagereward", "xpmr")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task XpMessageRewardAsync([Remainder] string? message = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
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

            var xpRoles = await DbContext.GetOrderedListAsync<GuildXpRoleEntity, int>(x => x.GuildId == Context.Guild!.Id, x => x.Level);

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
                await Context.Channel.SendMessageAsync(reply, customMessage.Embed);
            }
            else
            {
                reply += $"\n{GetText(Localization.XpNotificationMessage)}\n\n{xpMessageReward}";
                await Context.Channel.SendMessageAsync(reply);
            }
        }

        [Command("leveluprolereward", "lurr")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.ManageRoles)]
        [BotPermission(Permissions.ManageRoles)]
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

            var xpRoleDb = await DbContext.GetOrAddAsync(
                x => x.GuildId == Context.Guild!.Id && (x.Level == level || x.RoleId == role.Id),
                () => new GuildXpRoleEntity { GuildId = Context.Guild!.Id });
            xpRoleDb.Level = level;
            xpRoleDb.RoleId = role.Id;
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.XpLevelUpRoleRewardSet, role.Name, level);
        }

        [Command("leveluprolerewardlist", "lurrl", "lurrs")]
        [Context(ContextType.Guild)]
        [BotPermission(Permissions.ManageRoles)]
        public async Task LevelUpRoleRewardListAsync()
        {
            var levelRoles = await DbContext.GetOrderedListAsync<GuildXpRoleEntity, int>(x => x.GuildId == Context.Guild!.Id, y => y.Level);
            DbContext.RemoveRange(levelRoles.Where(x => Context.Guild!.GetRole(x.RoleId) is null));
            await DbContext.SaveChangesAsync();
            
            if (levelRoles.Count == 0)
            {
                await ReplyErrorAsync(Localization.XpNoLevelUpRoleReward);
                return;
            }

            await SendPaginatedMessageAsync(levelRoles, 15, (items, _) => new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpLevelUpRoleRewardList),
                Description = string.Join('\n', items.Select(lr => $"{GetText(Localization.XpLevelX, lr.Level)}: {Context.Guild!.GetRole(lr.RoleId).Mention}"))
            });
        }

        [Command("resetserverxp", "rsxp")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task ResetGuildXpAsync()
        {
            var componentInteractionArgs = await SendConfirmationButtonsAsync(Localization.XpResetGuildXpConfirmation);
            if (componentInteractionArgs is null)
                return;

            DbContext.RemoveRange(await DbContext.GetListAsync<MembersEntity>(x => x.GuildId == Context.Guild!.Id));
            await DbContext.SaveChangesAsync();
            await ButtonsActionModifyDescriptionAsync(componentInteractionArgs.Value.Result.Message, Localization.XpGuildXpReset);
        }

        [Command("xpignore", "xpi")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        [Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task XpIgnoreAsync([Remainder] DiscordChannel? channel = null)
        {
            channel ??= Context.Channel;

            if (channel.Type == ChannelType.Voice)
            {
                await ReplyErrorAsync(Localization.XpIgnoreVoiceChannelNotAllowed);
                return;
            }

            if (channel.IsCategory)
            {
                var textChannels = channel.Children.Where(c => c.Type is ChannelType.Text or ChannelType.News or ChannelType.Store).ToList();
                if (textChannels.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationNoTextChannelInCategory, channel.Name);
                    return;
                }
                
                var noChannelIgnored = true;
                
                var guildDb = await DbContext.GetOrAddAsync(g => g.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
                if (guildDb.XpIgnoredChannels is not null)
                {
                    var xpIgnoredChannelsSet = new HashSet<ulong>(guildDb.XpIgnoredChannels);
                    if (textChannels.Any(child => xpIgnoredChannelsSet.Contains(child.Id)))
                        noChannelIgnored = false;
                }

                if (noChannelIgnored)
                {
                    foreach (var textChannel in textChannels)
                        await Service.AddChannelToExclusionAsync(textChannel);
                    
                    await ReplyConfirmationAsync(Localization.XpChannelsIgnored, channel.Name);
                }
                else
                {
                    foreach (var textChannel in textChannels)
                        await Service.RemoveChannelFromExclusionAsync(textChannel);
                    
                    await ReplyConfirmationAsync(Localization.XpChannelsNotIgnored, channel.Name);
                }
            }
            else
            {
                if (Service.CheckExcludedChannel(channel))
                {
                    await Service.RemoveChannelFromExclusionAsync(channel);
                
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync(Localization.XpCurrentChannelNotIgnored);
                    else
                        await ReplyConfirmationAsync(Localization.XpChannelNotIgnored, channel.Name);
                }
                else
                {
                    await Service.AddChannelToExclusionAsync(channel);
                
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync(Localization.XpCurrentChannelIgnored);
                    else
                        await ReplyConfirmationAsync(Localization.XpChannelIgnored, channel.Name);
                }
            }
        }

        [Command("xpignorelist", "xpilist", "xpil")]
        [Context(ContextType.Guild)]
        [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task XpIgnoreList()
        {
            var guildDb = await DbContext.GetOrAddAsync(g => g.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            if (guildDb.XpIgnoredChannels is null)
            {
                await ReplyConfirmationAsync(Localization.XpNoIgnoredChannels);
                return;
            }

            var ignoredChannelsGroup = guildDb.XpIgnoredChannels
                .Select(xpic => Context.Guild!.GetChannel(xpic))
                .GroupBy(c => c.Parent)
                .OrderBy(x => x.Key?.Position);

            var list = new List<string>();
            foreach (var group in ignoredChannelsGroup)
            {
                if (group.Key is not null)
                    list.Add(Formatter.Bold(group.Key.Name));

                var ignoredChannels = group.OrderBy(c => c.Position).ToList();
                for (var i = 0; i < ignoredChannels.Count; i++)
                {
                    var ignoredChannel = ignoredChannels[i];
                    
                    if (ignoredChannels.Count == 1 && ignoredChannel.Parent is null)
                        list.Add($"\u2500\u2500{ignoredChannel.Name}");
                    else if (i == 0 && ignoredChannel.Parent is null)
                        list.Add($"\u250C\u2500{ignoredChannel.Name}");
                    else if (i == ignoredChannels.Count - 1)
                        list.Add($"\u2514\u2500{ignoredChannel.Name}");
                    else
                        list.Add($"\u251C\u2500{ignoredChannel.Name}");
                }
            }

            await SendPaginatedMessageAsync(list, 15, (items, _) => new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.XpIgnoredChannels),
                Description = string.Join('\n', items)
            });
        }

        [Command("setxpignorerole", "sxpir")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task SetXpIgnoreRoleAsync([Remainder] DiscordRole role)
        {
            if (role is { IsManaged: true })
            {
                await ReplyErrorAsync(Localization.XpIgnoredRoleNotSet, role.Mention);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(g => g.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            guildDb.XpIgnoredRoleId = role.Id;
            await DbContext.SaveChangesAsync();

            await ReplyConfirmationAsync(Localization.XpIgnoredRoleSet, role.Mention);
            
            if (!RiasBot.ChunkedGuilds.Contains(Context.Guild!.Id))
            {
                var tcs = new TaskCompletionSource();
                _botService.GuildsTcs[Context.Guild.Id] = tcs;
                
                RiasBot.ChunkedGuilds.Add(Context.Guild.Id);
                await Context.Guild.RequestMembersAsync();
                
                var delay = Task.Delay(30000);

                await Task.WhenAny(tcs.Task, delay);
                _botService.GuildsTcs.TryRemove(Context.Guild.Id, out _);
            }

            foreach (var (_, member) in Context.Guild.Members.Where(m => m.Value.Roles.Any(r => r.Id == role.Id)))
            {
                var memberDb = await DbContext.GetOrAddAsync(m => m.GuildId == Context.Guild.Id && m.MemberId == member.Id,
                    () => new MembersEntity { GuildId = Context.Guild.Id, MemberId = member.Id });
                memberDb.IsXpIgnored = true;
                await DbContext.SaveChangesAsync();
            }
        }

        [Command("xpignorerole", "xpir")]
        [Context(ContextType.Guild)]
        [Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task XpIgnoreRoleAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(g => g.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            if (guildDb.XpIgnoredRoleId != 0)
            {
                var ignoredRole = Context.Guild!.GetRole(guildDb.XpIgnoredRoleId);
                if (ignoredRole is not null)
                {
                    await ReplyConfirmationAsync(Localization.XpIgnoredRole, ignoredRole.Mention);
                    return;
                }
                
                foreach (var memberEntity in await DbContext.GetListAsync<MembersEntity>(m => m.GuildId == Context.Guild!.Id))
                    memberEntity.IsXpIgnored = false;
                
                guildDb.XpIgnoredRoleId = 0;
                await DbContext.SaveChangesAsync();
            }
            
            await ReplyErrorAsync(Localization.XpNoIgnoredRoleSet);
        }

        [Command("removexpignorerole", "rxpir")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        [Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task XpIgnoreRoleRemoveAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(g => g.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            if (guildDb.XpIgnoredRoleId != 0)
            {
                foreach (var memberEntity in await DbContext.GetListAsync<MembersEntity>(m => m.GuildId == Context.Guild!.Id))
                    memberEntity.IsXpIgnored = false;
                
                guildDb.XpIgnoredRoleId = 0;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.XpIgnoredRoleRemoved);
            }
            else
            {
                await ReplyErrorAsync(Localization.XpNoIgnoredRoleSet);
            }
        }
    }
}