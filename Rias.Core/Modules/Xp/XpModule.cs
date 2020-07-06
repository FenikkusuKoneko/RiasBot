using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
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
        public XpModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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
         UserPermission(Permission.Administrator)]
        public async Task XpNotificationAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            var guildXpNotification = guildDb.GuildXpNotification = !guildDb.GuildXpNotification;

            await DbContext.SaveChangesAsync();
            if (guildXpNotification)
                await ReplyConfirmationAsync(Localization.XpNotificationEnabled);
            else
                await ReplyConfirmationAsync(Localization.XpNotificationDisabled);
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