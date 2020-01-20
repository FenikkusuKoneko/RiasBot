using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Xp
{
    [Name("Xp")]
    public class Xp : RiasModule<XpService>
    {
        private readonly DiscordShardedClient _client;
        private readonly InteractiveService _interactive;
        
        public Xp(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _interactive = services.GetRequiredService<InteractiveService>();
        }
        
        [Command("xp"), Context(ContextType.Guild),
         Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task XpAsync(SocketGuildUser? user = null)
        {
            user ??= (SocketGuildUser) Context.User;
            using var unused = Context.Channel.EnterTypingState();

            await using var xpImage = await Service.GenerateXpImageAsync(user);
            await Context.Channel.SendFileAsync(xpImage, $"{user.Id}_xp.png");
        }

        [Command("xpleaderboard"),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task XpLeaderboardAsync(int page = 1)
        {
            page--;
            if (page < 0)
                page = 0;

            var xpLeaderboard = await DbContext.GetOrderedListAsync<Users, int>(x => x.Xp, true, (page * 9)..((page + 1) * 9));
            if (xpLeaderboard.Count == 0)
            {
                await ReplyErrorAsync("LeaderboardEmpty");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText("Leaderboard")
            };

            var index = page * 9;
            foreach (var userDb in xpLeaderboard)
            {
                var user = _client.GetUser(userDb.UserId);
                embed.AddField($"{++index}. {(user != null ? user.ToString() : userDb.UserId.ToString())}",
                    $"{GetText("LevelX", RiasUtils.XpToLevel(userDb.Xp, XpService.XpThreshold))} | {GetText("Xp")} {userDb.Xp}",
                    true);
            }

            await ReplyAsync(embed);
        }
        
        [Command("serverxpleaderboard"), Context(ContextType.Guild),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task GuildXpLeaderboardAsync(int page = 1)
        {
            page--;
            if (page < 0)
                page = 0;
            
            var xpLeaderboard = (await DbContext.GetOrderedListAsync<GuildsXp, int>(x => x.GuildId == Context.Guild!.Id, y => y.Xp, true))
                .Where(x => Context.Guild!.GetUser(x.UserId) != null)
                .Skip(page * 9)
                .Take(9)
                .ToList();
            
            if (xpLeaderboard.Count == 0)
            {
                await ReplyErrorAsync("GuildLeaderboardEmpty");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText("GuildLeaderboard")
            };

            var index = page * 9;
            foreach (var userDb in xpLeaderboard)
            {
                embed.AddField($"{++index}. {Context.Guild!.GetUser(userDb.UserId)}",
                    $"{GetText("LevelX", RiasUtils.XpToLevel(userDb.Xp, XpService.XpThreshold))} | {GetText("Xp")} {userDb.Xp}",
                    true);
            }

            await ReplyAsync(embed);
        }

        [Command("xpnotification"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task XpNotificationAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new Guilds {GuildId = Context.Guild!.Id});
            var guildXpNotification = guildDb.GuildXpNotification = !guildDb.GuildXpNotification;

            await DbContext.SaveChangesAsync();
            if (guildXpNotification)
                await ReplyConfirmationAsync("NotificationEnabled");
            else
                await ReplyConfirmationAsync("NotificationDisabled");
        }

        [Command("leveluprolereward"), Context(ContextType.Guild),
         UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles)]
        public async Task LevelUpRoleRewardAsync(int level, SocketRole? role = null)
        {
            if (level < 1)
            {
                await ReplyErrorAsync("LevelUpRoleRewardLimit");
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
                
                await ReplyConfirmationAsync("LevelUpRoleRewardRemoved", level);
                return;
            }
            
            if (role.IsManaged)
            {
                await ReplyErrorAsync("LevelUpRoleRewardNotSet", role.Name);
                return;
            }

            if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
            {
                await ReplyErrorAsync("#Administration_RoleAboveMe");
                return;
            }

            var xpRoleDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id && (x.Level == level || x.RoleId == role.Id),
                () => new GuildXpRoles {GuildId = Context.Guild!.Id});
            xpRoleDb.Level = level;
            xpRoleDb.RoleId = role.Id;
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("LevelUpRoleRewardSet", role.Name, level);
        }

        [Command("leveluprolerewardlist"), Context(ContextType.Guild),
         BotPermission(GuildPermission.ManageRoles)]
        public async Task LevelUpRoleRewardListAsync()
        {
            var levelRoles = await DbContext.GetListAsync<GuildXpRoles>(x => x.GuildId == Context.Guild!.Id);
            DbContext.RemoveRange(levelRoles.Where(x => Context.Guild!.GetRole(x.RoleId) is null));
            await DbContext.SaveChangesAsync();
            
            if (levelRoles.Count == 0)
            {
                await ReplyErrorAsync("NoLevelUpRoleReward");
                return;
            }

            var pages = levelRoles.OrderBy(x => x.Level).Batch(15, x => new InteractiveMessage
            (
                new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = GetText("LevelUpRoleRewardList"),
                    Description = string.Join('\n', x.Select(lr => $"{GetText("LevelX", lr.Level)}: {Context.Guild!.GetRole(lr.RoleId)}"))
                }
            ));

            await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
        }

        [Command("resetserverxp"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task ResetGuildXpAsync()
        {
            await ReplyConfirmationAsync("ResetGuildXpConfirmation");
            
            var message = await _interactive.NextMessageAsync(Context.Message);
            if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync("ResetGuildXpCanceled");
                return;
            }
            
            DbContext.RemoveRange(await DbContext.GetListAsync<GuildsXp>(x => x.GuildId == Context.Guild!.Id));
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("GuildXpReset");
        }
    }
}