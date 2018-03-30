using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Xp.Services;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Modules.Xp
{
    public partial class Xp : RiasModule<XpService>
    {
        private readonly CommandHandler _ch;
        private readonly DbService _db;
        public Xp (CommandHandler ch, DbService db)
        {
            _ch = ch;
            _db = db;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Experience([Remainder]IUser user = null)
        {
            user = user ?? Context.User;
            await Context.Channel.TriggerTypingAsync();

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                var guildXp = db.XpSystem.Where(x => x.GuildId == Context.Guild.Id);
                var xpDb = guildXp.Where(x => x.UserId == user.Id).FirstOrDefault();

                var globalRanks = db.Users.OrderByDescending(x => x.Xp).Select(y => y.Xp).ToList();
                var guildRanks = guildXp.OrderByDescending(x => x.Xp).Select(y => y.Xp).ToList();

                int globalRank = globalRanks.IndexOf(userDb?.Xp ?? -1) + 1;
                int guildRank = guildRanks.IndexOf(xpDb?.Xp ?? -1) + 1;

                int globalCurrentXp = 0;
                int globalCurrentLevel = 0;
                int globalRequiredXp = 0;
                int guildCurrentXp = 0;
                int guildCurrentLevel = 0;
                int guildRequiredXp = 0;

                try
                {
                    globalCurrentXp = userDb.Xp;
                    globalCurrentLevel = userDb.Level;
                }
                catch
                {
                    
                }
                try
                {
                    guildCurrentXp = xpDb.Xp;
                    guildCurrentLevel = xpDb.Level;
                }
                catch
                {
                    
                }

                while (globalCurrentXp >= 0)
                {
                    globalRequiredXp += 30;
                    globalCurrentXp -= globalRequiredXp;
                }

                while (guildCurrentXp >= 0)
                {
                    guildRequiredXp += 30;
                    guildCurrentXp -= guildRequiredXp;
                }

                var roles = new List<IRole>();
                var rolesIds = ((IGuildUser)user).RoleIds;
                foreach (var role in rolesIds)
                {
                    var r = Context.Guild.GetRole(role);
                    roles.Add(r);
                }
                var highestRole = roles.OrderByDescending(x => x.Position).Select(y => y).FirstOrDefault();

                using (var img = await _service.GenerateXpImage((IGuildUser)user, (globalCurrentLevel, guildCurrentLevel),
                    (globalCurrentXp + globalRequiredXp, guildCurrentXp + guildRequiredXp), ((globalRequiredXp, guildRequiredXp)), globalRank, guildRank, highestRole))
                {
                    await Context.Channel.SendFileAsync(img, $"{user.Id}_xp.png").ConfigureAwait(false);
                }
                return;
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task XpLeaderboard(int page = 1)
        {
            page--;
            using (var db = _db.GetDbContext())
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle("Global XP Leaderboard");

                var xps = db.Users
                    .GroupBy(x => new { x.Xp, x.UserId, x.Level })
                    .OrderByDescending(y => y.Key.Xp)
                    .Skip(page * 9).Take(9).ToList();

                for (int i = 0; i < xps.Count; i++)
                {
                    var user = await Context.Client.GetUserAsync(xps[i].Key.UserId);
                    embed.AddField($"#{i+1 + (page * 9)} {user?.ToString() ?? xps[i].Key.UserId.ToString()}", $"{xps[i].Key.Xp} xp\tlevel {xps[i].Key.Level}\n", true);
                }
                if (xps.Count == 0)
                    embed.WithDescription("No users on this page");

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task GuildXpLeaderboard(int page = 1)
        {
            page--;
            using (var db = _db.GetDbContext())
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle("Server XP Leaderboard");

                var xpSystemDb = db.XpSystem.Where(x => x.GuildId == Context.Guild.Id);
                var xps = xpSystemDb
                    .GroupBy(x => new { x.Xp, x.UserId, x.Level })
                    .OrderByDescending(y => y.Key.Xp)
                    .Skip(page * 9).Take(9).ToList();

                for (int i = 0; i < xps.Count; i++)
                {
                    var user = await Context.Client.GetUserAsync(xps[i].Key.UserId);
                    embed.AddField($"#{i + 1 + (page * 9)} {user?.ToString() ?? xps[i].Key.UserId.ToString()}", $"{xps[i].Key.Xp} xp\tlevel {xps[i].Key.Level}\n", true);
                }
                if (xps.Count == 0)
                    embed.WithDescription("No users on this page");

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task XpNotify()
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                bool xpNotify = false;
                try
                {
                    xpNotify = guildDb.XpGuildNotification;
                    guildDb.XpGuildNotification = !guildDb.XpGuildNotification;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                catch
                {
                    var xpNotifyDb = new GuildConfig { GuildId = Context.Guild.Id, XpGuildNotification = true };
                    await db.AddAsync(xpNotifyDb).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                if (!xpNotify)
                    await ReplyAsync($"{Context.User.Mention} Server xp notification enabled.");
                else
                    await ReplyAsync($"{Context.User.Mention} Server xp notification disabled.");
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task LevelUpRoleReward(int level, [Remainder]string name = null)
        {
            if (level <= 0)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you can't set a role reward for a level less or equal than 0.").ConfigureAwait(false);
                return;
            }
            using (var db = _db.GetDbContext())
            {
                var xpRolesSystem = db.XpRolesSystem.Where(x => x.GuildId == Context.Guild.Id).ToList();
                if (String.IsNullOrEmpty(name))
                {
                    if (db.XpRolesSystem.Any(x => x.Level == level))
                    {
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} no role reward for level {Format.Bold(level.ToString())}").ConfigureAwait(false);
                        var oldRoleReward = xpRolesSystem.Where(x => x.Level == level).FirstOrDefault();
                        db.Remove(oldRoleReward);
                    }
                }
                else
                {
                    var role = Context.Guild.Roles.Where(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
                    if (role != null)
                    {
                        if (db.XpRolesSystem.Any(x => x.Level == level))
                        {
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} at level {Format.Bold(level.ToString())} " +
                                $"the users will get {Format.Bold(role.Name)} role").ConfigureAwait(false);
                            var newRoleReward = xpRolesSystem.Where(x => x.Level == level).FirstOrDefault();
                            newRoleReward.RoleId = role.Id;
                        }
                        else
                        {
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} at level {Format.Bold(level.ToString())} " +
                                $"the users will get {Format.Bold(role.Name)} role").ConfigureAwait(false);
                            var roleReward = new XpRolesSystem { GuildId = Context.Guild.Id, Level = level, RoleId = role.Id};
                            await db.AddAsync(roleReward).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                    }
                }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
