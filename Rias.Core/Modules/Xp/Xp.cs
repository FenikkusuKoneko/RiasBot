using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
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
        private readonly InteractiveService _interactive;
        
        public Xp(IServiceProvider services) : base(services)
        {
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
            
            var xpLeaderboard = Service.GetXpLeaderboard(page * 9, 9);
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
                var user = Context.Client.GetUser(userDb.UserId);
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
            
            var xpLeaderboard = Service.GetGuildXpLeaderboard(Context.Guild!)
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
            var guildXpNotification = await Service.SetGuildXpNotificationAsync(Context.Guild!);
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
                await Service.SetLevelRoleAsync(Context.Guild!, level, null);
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
                await ReplyErrorAsync("#Administration_RoleAbove");
                return;
            }
            
            await Service.SetLevelRoleAsync(Context.Guild!, level, role);
            await ReplyConfirmationAsync("LevelUpRoleRewardSet", role.Name, level);
        }

        [Command("leveluprolerewardlist"), Context(ContextType.Guild),
         BotPermission(GuildPermission.ManageRoles)]
        public async Task LevelUpRoleRewardListAsync()
        {
            var levelRoles = await Service.UpdateLevelRolesAsync(Context.Guild!);
            if (levelRoles.Count == 0)
            {
                await ReplyErrorAsync("NoLevelUpRoleReward");
                return;
            }

            var pages = levelRoles.Values
                .OrderBy(x => x.Level)
                .Batch(15, x => new InteractiveMessage
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

            await Service.ResetGuildXp(Context.Guild!);
            await ReplyConfirmationAsync("GuildXpReset");
        }
    }
}