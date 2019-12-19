using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Services
{
    public class XpService : RiasService
    {
        private readonly HttpClient _httpClient;
        
        public XpService(IServiceProvider services) : base(services)
        {
            _httpClient = Services.GetRequiredService<HttpClient>();
        }

        public const int XpThreshold = 30;
        
        private readonly MagickColor _dark = MagickColor.FromRgb(36, 36, 36);
        private readonly MagickColor _darker = MagickColor.FromRgb(32, 32, 32);
        
        private readonly string _arialFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/ArialBold.ttf");
        private readonly string _meiryoFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/Meiryo.ttf");

        public IList<Users> GetXpLeaderboard(int skip, int take)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return db.Users
                .OrderByDescending(x => x.Xp)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
        
        public IList<GuildsXp> GetGuildXpLeaderboard(SocketGuild guild)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return db.GuildsXp
                .Where(x => x.GuildId == guild.Id)
                .OrderByDescending(x => x.Xp)
                .ToList();
        }

        public async Task<bool> SetGuildXpNotificationAsync(SocketGuild guild)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                 var guildXpNotification = guildDb.GuildXpNotification = !guildDb.GuildXpNotification;
                 await db.SaveChangesAsync();
                 
                 return guildXpNotification;
            }

            var newGuildDb = new Guilds {GuildId = guild.Id, GuildXpNotification = true};
            await db.AddAsync(newGuildDb);
            await db.SaveChangesAsync();
                
            return true;
        }

        public async Task SetLevelRoleAsync(SocketGuild guild, int level , SocketRole? role)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var xpRoleLevelDb = db.GuildXpRoles.FirstOrDefault(x => x.GuildId == guild.Id && x.Level == level);
            if (role is null)
            {
                
                if (xpRoleLevelDb is null)
                    return;

                db.Remove(xpRoleLevelDb);
                await db.SaveChangesAsync();
                return;
            }
            
            var xpRoleDb = db.GuildXpRoles.FirstOrDefault(x => x.GuildId == guild.Id && x.RoleId == role.Id);
            if (xpRoleDb != null)
            {
                if (xpRoleLevelDb != null)
                {
                    xpRoleLevelDb.RoleId = role.Id;
                    db.Remove(xpRoleDb);
                }
                else
                {
                    xpRoleDb.Level = level;
                }
            }
            else
            {
                if (xpRoleLevelDb != null)
                {
                    xpRoleLevelDb.RoleId = role.Id;
                }
                else
                {
                    var newXpRoleDb = new GuildXpRoles {GuildId = guild.Id, Level = level, RoleId = role.Id};
                    await db.AddAsync(newXpRoleDb);
                }
            }

            await db.SaveChangesAsync();
        }

        public async Task<IDictionary<ulong, GuildXpRoles>> UpdateLevelRolesAsync(SocketGuild guild)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var lrDict = db.GuildXpRoles.Where(x => x.GuildId == guild.Id).ToDictionary(x => x.RoleId);

            foreach (var (lrKey, lrValue) in lrDict)
            {
                var role = guild.GetRole(lrKey);
                if (role != null) continue;
                
                lrDict.Remove(lrKey);
                db.Remove(lrValue);
            }

            await db.SaveChangesAsync();
            return lrDict;
        }

        public async Task ResetGuildXp(SocketGuild guild)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            db.RemoveRange(db.GuildsXp.Where(x => x.GuildId == guild.Id));
            await db.SaveChangesAsync();
        }
        
        public async Task AddUserXpAsync(SocketUserMessage userMessage)
        {
            if (!(userMessage.Author is SocketGuildUser guildUser))
                return;
            
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == guildUser.Id);

            var now = DateTime.UtcNow;
            if (userDb != null)
            {
                if (userDb.IsBlacklisted)
                    return;
                
                if (userDb.LastMessageDate + TimeSpan.FromMinutes(5) > now)
                    return;
                
                userDb.Xp += 5;
                userDb.LastMessageDate = now;
            }
            else
            {
                var newGuildXpDb = new GuildsXp {UserId = guildUser.Id, LastMessageDate = now};
                await db.AddAsync(newGuildXpDb);
            }

            await db.SaveChangesAsync();
        }

        public async Task AddGuildUserXpAsync(SocketUserMessage userMessage)
        {
            if (!(userMessage.Author is SocketGuildUser guildUser))
                return;
            
            var (currentLevel, nextLevel) = (0, 0);
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildXpDb = db.GuildsXp.FirstOrDefault(x => x.GuildId == guildUser.Guild.Id && x.UserId == guildUser.Id);

            var now = DateTime.UtcNow;
            if (guildXpDb != null)
            {
                if (guildXpDb.LastMessageDate + TimeSpan.FromMinutes(5) > now)
                    return;

                currentLevel = RiasUtils.XpToLevel(guildXpDb.Xp, 30);
                guildXpDb.Xp += 5;
                guildXpDb.LastMessageDate = now;
                nextLevel = RiasUtils.XpToLevel(guildXpDb.Xp, 30);
            }
            else
            {
                var newGuildXpDb = new GuildsXp {GuildId = guildUser.Guild.Id, UserId = guildUser.Id, LastMessageDate = now};
                await db.AddAsync(newGuildXpDb);
            }

            await db.SaveChangesAsync();
            if (currentLevel != nextLevel)
                await UserLevelUpAsync(guildUser, userMessage.Channel, nextLevel);
        }

        private async Task UserLevelUpAsync(SocketGuildUser user, IMessageChannel channel, int level)
        {
            var guild = user.Guild;
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);

            var currentUser = guild.CurrentUser;
            if (!currentUser.GuildPermissions.ManageRoles)
            {
                if (guildDb != null && guildDb.GuildXpNotification)
                    await ReplyConfirmationAsync(channel, user.Guild.Id, "Xp", "GuildLevelUp", user, level);
                
                return;
            }
            
            var xpRoleDb = db.GuildXpRoles.FirstOrDefault(x => x.GuildId == guild.Id && x.Level == level);
            if (xpRoleDb is null)
            {
                if (guildDb != null && guildDb.GuildXpNotification)
                    await ReplyConfirmationAsync(channel, user.Guild.Id, "Xp", "GuildLevelUp", user, level);
                
                return;
            }

            var role = guild.GetRole(xpRoleDb.RoleId);
            if (role is null)
            {
                if (guildDb != null && guildDb.GuildXpNotification)
                    await ReplyConfirmationAsync(channel, user.Guild.Id, "Xp", "GuildLevelUp", user, level);
                
                return;
            }

            if (currentUser.CheckRoleHierarchy(role) > 0 && !role.IsManaged && user.Roles.All(x => x.Id != role.Id))
            {
                await user.AddRoleAsync(role);
                if (guildDb != null && guildDb.GuildXpNotification)
                    await ReplyConfirmationAsync(channel, user.Guild.Id, "Xp", "GuildLevelUpRoleReward", user, level, role);
            }
            else
            {
                if (guildDb != null && guildDb.GuildXpNotification)
                    await ReplyConfirmationAsync(channel, user.Guild.Id, "Xp", "GuildLevelUp", user, level);
            }
        }
        public async Task<Stream> GenerateXpImageAsync(SocketGuildUser user)
        {
            var xpInfo = GetXpInfo(user);

            using var image = new MagickImage(_dark, 500, 300);

            await AddAvatarAndUsernameAsync(image, user);
            AddInfo(image, xpInfo, user.Guild);

            var imageStream = new MemoryStream();
            image.Write(imageStream, MagickFormat.Png);
            imageStream.Position = 0;
            return imageStream;
        }
        
        private async Task AddAvatarAndUsernameAsync(MagickImage image, SocketUser user)
        {
            await using var avatarStream = await _httpClient.GetStreamAsync(user.GetRealAvatarUrl());
            using var avatarImage = new MagickImage(avatarStream);
            avatarImage.Resize(new MagickGeometry
            {
                Width = 70,
                Height = 70
            });
            
            using var avatarLayer = new MagickImage(MagickColors.Transparent, 70, 70);
            avatarLayer.Draw(new Drawables().RoundRectangle(0, 0, avatarLayer.Width, avatarLayer.Height, 15, 15).FillColor(MagickColors.White));
            avatarImage.Composite(avatarLayer, CompositeOperator.DstIn);
            avatarLayer.Draw(new DrawableComposite(0, 0, CompositeOperator.Over, avatarImage));
            
            image.Draw(new DrawableComposite(30, 30, CompositeOperator.Over, avatarLayer));
            
            var usernameSettings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.White,
                Font = _meiryoFontPath,
                FontWeight = FontWeight.Bold,
                Width = 300,
                Height = 50
            };
            
            using var usernameImage = new MagickImage($"caption:{user}", usernameSettings);
            image.Draw(new DrawableComposite(120, 45, CompositeOperator.Over, usernameImage));
        }

        private void AddInfo(MagickImage image, XpInfo xpInfo, SocketGuild guild)
        {
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.White,
                Font = _arialFontPath,
                FontPointsize = 15,
                Width = 100
            };

            using var globalTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Common", "Global").ToUpperInvariant()}", settings);
            image.Draw(new DrawableComposite(30, 120, CompositeOperator.Over, globalTextImage));
            using var serverTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Common", "Server").ToUpperInvariant()}", settings);
            image.Draw(new DrawableComposite(30, 210, CompositeOperator.Over, serverTextImage));

            settings.TextGravity = Gravity.Center;
            using var globalLevelTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Xp", "Lvl", xpInfo.GlobalLevel).ToUpperInvariant()}", settings);
            image.Draw(new DrawableComposite(250 - (double) globalLevelTextImage.Width / 2, 120, CompositeOperator.Over, globalLevelTextImage));
            using var serverLevelTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Xp", "Lvl", xpInfo.ServerLevel).ToUpperInvariant()}", settings);
            image.Draw(new DrawableComposite(250 - (double) serverLevelTextImage.Width / 2, 210, CompositeOperator.Over, serverLevelTextImage));

            settings.TextGravity = Gravity.East;
            using var globalRankTextImage = new MagickImage($"caption:#{xpInfo.GlobalRank}", settings);
            image.Draw(new DrawableComposite(470 - (double) globalRankTextImage.Width, 120, CompositeOperator.Over, globalRankTextImage));
            using var serverRankTextImage = new MagickImage($"caption:#{xpInfo.ServerRank}", settings);
            image.Draw(new DrawableComposite(470 - (double) serverRankTextImage.Width, 210, CompositeOperator.Over, serverRankTextImage));
            
            image.Draw(new Drawables()
                .RoundRectangle(30, 150, 470, 160, 5, 5)
                .FillColor(_darker));
            image.Draw(new Drawables()
                .RoundRectangle(30, 240, 470, 250, 5, 5)
                .FillColor(_darker));

            var globalCurrentXp = RiasUtils.LevelXp(xpInfo.GlobalLevel, xpInfo.GlobalXp, XpThreshold);
            var globalNextLevelXp = (xpInfo.GlobalLevel + 1) * 30;
            
            var globalXpBarLength = (double) globalCurrentXp  / globalNextLevelXp * 440;
            image.Draw(new Drawables()
                .RoundRectangle(30, 150, 30 + globalXpBarLength, 160, 5, 5)
                .FillColor(xpInfo.Color));
            
            var serverCurrentXp = RiasUtils.LevelXp(xpInfo.ServerLevel, xpInfo.ServerXp, XpThreshold);
            var serverNextLevelXp = (xpInfo.ServerLevel + 1) * 30;
            
            var serverXpBarLength = (double) serverCurrentXp  / serverNextLevelXp * 440;
            image.Draw(new Drawables()
                .RoundRectangle(30, 240, 30 + serverXpBarLength, 250, 5, 5)
                .FillColor(xpInfo.Color));

            settings.FontPointsize = 12;
            settings.TextGravity = Gravity.West;
            using var globalCurrentXpTextImage = new MagickImage($"caption:{globalCurrentXp}", settings);
            image.Draw(new DrawableComposite(30, 170, CompositeOperator.Over, globalCurrentXpTextImage));
            using var serverCurrentXpTextImage = new MagickImage($"caption:{serverCurrentXp}", settings);
            image.Draw(new DrawableComposite(30, 260, CompositeOperator.Over, serverCurrentXpTextImage));
            
            settings.TextGravity = Gravity.East;
            using var globalNextLevelXpTextImage = new MagickImage($"caption:{globalNextLevelXp}", settings);
            image.Draw(new DrawableComposite(470 - (double) globalNextLevelXpTextImage.Width, 170, CompositeOperator.Over, globalNextLevelXpTextImage));
            using var serverNextLevelXpTextImage = new MagickImage($"caption:{serverNextLevelXp}", settings);
            image.Draw(new DrawableComposite(470 - (double) serverNextLevelXpTextImage.Width, 260, CompositeOperator.Over, serverNextLevelXpTextImage));
        }

        private XpInfo GetXpInfo(SocketGuildUser user)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            var serverXpDb = db.GuildsXp.FirstOrDefault(x => x.GuildId == user.Guild.Id && x.UserId == user.Id);
            var profileDb = db.Profile.FirstOrDefault(x => x.UserId == user.Id);

            var globalXp = userDb?.Xp ?? 0;
            var serverXp = serverXpDb?.Xp ?? 0;
            return new XpInfo
            {
                GlobalXp = globalXp,
                GlobalLevel = RiasUtils.XpToLevel(globalXp, XpThreshold),
                GlobalRank = userDb != null
                    ? db.Users.Select(x => x.Xp)
                          .OrderByDescending(y => y)
                          .ToList()
                          .IndexOf(userDb.Xp) + 1
                    : 0,
                ServerXp = serverXp,
                ServerLevel = RiasUtils.XpToLevel(serverXp, XpThreshold),
                ServerRank = serverXpDb != null
                    ? db.GuildsXp.Where(x => x.GuildId == user.Guild.Id)
                        .Select(x => x.Xp)
                        .OrderByDescending(y => y)
                        .ToList()
                        .IndexOf(serverXpDb.Xp) + 1
                    : 0,
                Color = profileDb?.Color != null ? new MagickColor($"{profileDb.Color}") : MagickColors.White,
            };
        }

        private class XpInfo
        {
            public int GlobalXp { get; set; }
            public int GlobalLevel { get; set; }
            public int GlobalRank { get; set; }
            public int ServerXp { get; set; }
            public int ServerLevel { get; set; }
            public int ServerRank { get; set; }
            public MagickColor? Color { get; set; }
        }
    }
}