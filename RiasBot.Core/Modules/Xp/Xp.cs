using Discord;
using Discord.Addons.Interactive;
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
        private readonly InteractiveService _is;
        private readonly DbService _db;
        public Xp (CommandHandler ch, InteractiveService iss,DbService db)
        {
            _ch = ch;
            _is = iss;
            _db = db;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(1, 30, Measure.Seconds, applyPerGuild: true)]
        public async Task Experience([Remainder]IUser user = null)
        {
            user = user ?? Context.User;
            await Context.Channel.TriggerTypingAsync();

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                var guildXp = db.XpSystem.Where(x => x.GuildId == Context.Guild.Id).ToList();
                var guildXpList = new List<XpSystem>();
                guildXp.ForEach(async x =>
                {
                    var userXp = await Context.Guild.GetUserAsync(x.UserId);
                    if (userXp != null)
                        guildXpList.Add(x);
                });

                var xpDb = guildXpList.Where(x => x.UserId == user.Id).FirstOrDefault();

                var globalRanks = db.Users.OrderByDescending(x => x.Xp).Select(y => y.Xp).ToList();
                var guildRanks = guildXpList.OrderByDescending(x => x.Xp).Select(y => y.Xp).ToList();

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
                    try
                    {
                        await Context.Channel.SendFileAsync(img, $"{user.Id}_xp.png").ConfigureAwait(false);
                    }
                    catch
                    {

                    }
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task XpLeaderboard(int page = 1)
        {
            page--;
            using (var db = _db.GetDbContext())
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle("Global XP Leaderboard");

                var xpUser = new List<string>();

                var xps = db.Users
                    .GroupBy(x => new { x.Xp, x.UserId, x.Level })
                    .OrderByDescending(y => y.Key.Xp)
                    .Skip(page * 10).Take(10).ToList();

                for (int i = 0; i < xps.Count; i++)
                {
                    var user = await Context.Client.GetUserAsync(xps[i].Key.UserId);
                    if (user != null)
                        xpUser.Add(($"#{i+1 + (page * 10)} {user} ({user.Id})\n\t\t{xps[i].Key.Xp} xp\tlevel {xps[i].Key.Level}\n"));
                    else
                        xpUser.Add(($"#{i+1 + (page * 10)} {xps[i].Key.UserId.ToString()}\n\t\t{xps[i].Key.Xp} xp\tlevel {xps[i].Key.Level}\n"));
                }
                if (xps.Count == 0)
                    embed.WithDescription("No users on this page");
                else
                    embed.WithDescription(String.Join('\n', xpUser));

                await Context.Channel.SendMessageAsync("", embed: embed.Build());
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

                var xpUser = new List<string>();

                var xpSystemDb = db.XpSystem.Where(x => x.GuildId == Context.Guild.Id).ToList();
                var xpSystemDbList = new List<XpSystem>();
                xpSystemDb.ForEach(async x =>
                {
                    var user = await Context.Guild.GetUserAsync(x.UserId);
                    if (user != null)
                        xpSystemDbList.Add(x);
                });
                    
                var xps = xpSystemDbList
                    .GroupBy(x => new { x.Xp, x.UserId, x.Level })
                    .OrderByDescending(y => y.Key.Xp)
                    .Skip(page * 10).Take(10).ToList();

                for (int i = 0; i < xps.Count; i++)
                {
                    var user = await Context.Client.GetUserAsync(xps[i].Key.UserId);
                    if (user != null)
                        xpUser.Add(($"#{i + 1 + (page * 10)} {user} ({user.Id})\n\t\t{xps[i].Key.Xp} xp\tlevel {xps[i].Key.Level}\n"));
                    else
                        xpUser.Add(($"#{i + 1 + (page * 10)} {xps[i].Key.UserId.ToString()}\n\t\t{xps[i].Key.Xp} xp\tlevel {xps[i].Key.Level}\n"));
                }
                if (xps.Count == 0)
                    embed.WithDescription("No users on this page");
                else
                    embed.WithDescription(String.Join('\n', xpUser));

                await Context.Channel.SendMessageAsync("", embed: embed.Build());
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
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} Server xp notification enabled.");
                else
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} Server xp notification disabled.");
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
                var xpRolesSystem = db.XpRolesSystem.Where(x => x.GuildId == Context.Guild.Id);
                if (String.IsNullOrEmpty(name))
                {

                    var oldRoleReward = xpRolesSystem?.Where(x => x.Level == level).FirstOrDefault();
                    if (oldRoleReward != null)
                    {
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} no role reward for level {Format.Bold(level.ToString())}").ConfigureAwait(false);
                        db.Remove(oldRoleReward);
                    }
                    else
                    {
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the level {Format.Bold(level.ToString())} doesn't have a role reward.").ConfigureAwait(false);
                    }
                }
                else
                {
                    var role = Context.Guild.Roles.Where(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
                    if (role != null)
                    {
                        var newRoleReward = xpRolesSystem?.Where(x => x.Level == level).FirstOrDefault();
                        if (newRoleReward != null)
                        {
                            newRoleReward.RoleId = role.Id;
                        }
                        else
                        {
                            var roleReward = new XpRolesSystem { GuildId = Context.Guild.Id, Level = level, RoleId = role.Id };
                            await db.AddAsync(roleReward).ConfigureAwait(false);
                        }
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} at level {Format.Bold(level.ToString())} " +
                                $"the users will get {Format.Bold(role.Name)} role").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                    }
                }

                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task LevelUpRoleRewards()
        {
            using (var db = _db.GetDbContext())
            {
                var xpRolesSystem = db.XpRolesSystem.Where(x => x.GuildId == Context.Guild.Id);
                if (xpRolesSystem != null)
                {
                    xpRolesSystem = xpRolesSystem.OrderBy(x => x.Level);
                    var lurrs = new List<string>();
                    foreach (var lurr in xpRolesSystem)
                    {
                        var role = Context.Guild.GetRole(lurr.RoleId);
                        if (role != null)
                            lurrs.Add($"Level {lurr.Level}:\t{role.Name}");
                    }
                    var pager = new PaginatedMessage
                    {
                        Title = $"All levelup role rewards on this server",
                        Color = new Color(RiasBot.goodColor),
                        Pages = lurrs,
                        Options = new PaginatedAppearanceOptions
                        {
                            ItemsPerPage = 10,
                            Timeout = TimeSpan.FromMinutes(1),
                            DisplayInformationIcon = false,
                            JumpDisplayOptions = JumpDisplayOptions.Never
                        }

                    };
                    await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} no levelup role rewards on this server.").ConfigureAwait(false);
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task ResetGuildExperience()
        {
            using (var db = _db.GetDbContext())
            {
                var xpSystemDb = db.XpSystem.Where(x => x.GuildId == Context.Guild.Id);
                foreach (var xpData in xpSystemDb)
                {
                    db.Remove(xpData);
                }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} server xp leaderboard has been reset.");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task RemoveGlobalExperience(int level, int amount, ulong id)
        {
            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == id).FirstOrDefault();
                if (userDb != null)
                {
                    if (userDb.Xp - amount < 0)
                    {
                        return;
                    }
                    else
                    {
                        userDb.Xp -= amount;
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    var user = await Context.Client.GetUserAsync(id).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed($"Took {amount} global xp from {Format.Bold(user.ToString())}, current level {level}.").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("The user doesn't exists in the database");
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task RemoveGlobalExperience(int level, int amount, string user)
        {
            var userSplit = user.Split("#");
            var getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                if (userDb != null)
                {
                    if (userDb.Xp - amount < 0)
                    {
                        return;
                    }
                    else
                    {
                        userDb.Level = level;
                        userDb.Xp -= amount;
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed($"Took {amount} global xp from {Format.Bold(getUser.ToString())}, current level {level}.").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("The user doesn't exists in the database");
                }
            }
        }
    }
}
