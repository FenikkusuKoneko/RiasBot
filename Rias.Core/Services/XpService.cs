using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Services
{
    public class XpService : RiasService
    {
        private readonly HttpClient _httpClient;
        
        public XpService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }
        
        public const int XpThreshold = 30;
        
        private readonly ConcurrentDictionary<ulong, DateTime> _usersXp = new ConcurrentDictionary<ulong, DateTime>();
        private readonly ConcurrentDictionary<(ulong, ulong), DateTime> _guildUsersXp = new ConcurrentDictionary<(ulong, ulong), DateTime>();

        private readonly MagickColor _dark = MagickColor.FromRgb(36, 36, 36);
        private readonly MagickColor _darker = MagickColor.FromRgb(32, 32, 32);
        
        private readonly string _arialFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/ArialBold.ttf");
        private readonly string _meiryoFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/Meiryo.ttf");
        
        public async Task AddUserXpAsync(CachedUser user)
        {
            var now = DateTime.UtcNow;
            var check = false;
            
            if (_usersXp.TryGetValue(user.Id, out var cooldown))
            {
                if (cooldown + TimeSpan.FromMinutes(5) > now)
                    return;
            }
            else
            {
                check = true;
            }
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id, Xp = -5});
            
            if (check && userDb.LastMessageDate + TimeSpan.FromMinutes(5) > now)
                return;
            
            userDb.Xp += 5;
            userDb.LastMessageDate = now;
            
            await db.SaveChangesAsync();
            _usersXp[user.Id] = now;
        }
        
        public async Task AddGuildUserXpAsync(CachedMember member, IMessageChannel channel)
        {
            var now = DateTime.UtcNow;
            var check = false;
            
            if (_guildUsersXp.TryGetValue((member.Guild.Id, member.Id), out var cooldown))
            {
                if (cooldown + TimeSpan.FromMinutes(5) > now)
                    return;
            }
            else
            {
                check = true;
            }
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildXpDb = await db.GetOrAddAsync(x => x.GuildId == member.Guild.Id && x.UserId == member.Id,
                () => new GuildUsersEntity {GuildId = member.Guild.Id, UserId = member.Id});
            
            if (check && guildXpDb.LastMessageDate + TimeSpan.FromMinutes(5) > now)
                return;
            
            var currentLevel = RiasUtilities.XpToLevel(guildXpDb.Xp, 30);
            guildXpDb.Xp += 5;
            guildXpDb.LastMessageDate = now;
            var nextLevel = RiasUtilities.XpToLevel(guildXpDb.Xp, 30);

            await db.SaveChangesAsync();
            _guildUsersXp[(member.Guild.Id, member.Id)] = now;

            if (currentLevel == nextLevel)
                return;
            
            var guild = member.Guild;
            CachedRole? role = null;
            
            var currentMember = guild.CurrentMember;
            if (currentMember.Permissions.ManageRoles)
            {
                var xpRoleDb = await db.GuildXpRoles.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.Level == nextLevel);
                if (xpRoleDb != null)
                {
                    role = guild.GetRole(xpRoleDb.RoleId);
                    if (role != null && member.Roles.All(x => x.Key != role.Id))
                        await member.GrantRoleAsync(role.Id);
                }
            }
            
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            if (guildDb != null && guildDb.XpNotification)
                await SendXpNotificationAsync(member, (CachedTextChannel) channel, role, nextLevel, guildDb.XpWebhookId);
        }
        
        private async Task SendXpNotificationAsync(CachedMember member, CachedTextChannel channel, CachedRole? role, int level, ulong xpWebhookId)
        {
            var guild = member.Guild;
            var currentMember = guild.CurrentMember;
            
            if (xpWebhookId == 0)
            {
                if (role is null)
                {
                    if (currentMember.GetPermissionsFor(channel).SendMessages)
                        await ReplyConfirmationAsync(channel, guild.Id, Localization.XpGuildLevelUp, member, level);
                }
                else
                {
                    if (currentMember.Permissions.ManageWebhooks)
                        await ReplyConfirmationAsync(channel, guild.Id, Localization.XpGuildLevelUpRoleReward, member, level, role);
                }
            }
            else if (currentMember.Permissions.ManageWebhooks)
            {
                var webhook = await guild.GetWebhookAsync(xpWebhookId);
                if (webhook is null)
                {
                    await DisableXpNotificationAsync(guild);
                    return;
                }
                
                var description = role is null
                    ? GetText(guild.Id, Localization.XpGuildLevelUp, member, level)
                    : GetText(guild.Id, Localization.XpGuildLevelUpRoleReward, member, level, role);
                
                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Description = description
                };
                
                await new RestWebhookClient(webhook).ExecuteAsync(embeds: new[] {embed.Build()});
            }
            else
            {
                await DisableXpNotificationAsync(guild);
            }
        }

        private async Task DisableXpNotificationAsync(CachedGuild guild)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
                    
            guildDb.XpNotification = false;
            await db.SaveChangesAsync();
        }

        public async Task<Stream> GenerateXpImageAsync(CachedMember member)
        {
            var xpInfo = await GetXpInfo(member);

            using var image = new MagickImage(_dark, 500, 300);

            await AddAvatarAndUsernameAsync(image, member);
            AddInfo(image, xpInfo, member.Guild);

            var imageStream = new MemoryStream();
            image.Write(imageStream, MagickFormat.Png);
            imageStream.Position = 0;
            return imageStream;
        }
        
        private async Task AddAvatarAndUsernameAsync(MagickImage image, CachedUser user)
        {
            await using var avatarStream = await _httpClient.GetStreamAsync(user.GetAvatarUrl());
            using var avatarImage = new MagickImage(avatarStream);
            avatarImage.Resize(new MagickGeometry
            {
                Width = 70,
                Height = 70
            });
            
            using var avatarLayer = new MagickImage(MagickColors.Transparent, 70, 70);
            avatarLayer.Draw(new Drawables().RoundRectangle(0, 0, avatarLayer.Width, avatarLayer.Height, 15, 15)
                .FillColor(MagickColors.White));
            avatarLayer.Composite(avatarImage, CompositeOperator.Atop);
            
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
        
        private void AddInfo(MagickImage image, XpInfo xpInfo, CachedGuild guild)
        {
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.White,
                Font = _arialFontPath,
                FontPointsize = 15,
                Width = 100
            };

            using var globalTextImage = new MagickImage($"caption:{GetText(guild.Id, Localization.CommonGlobal).ToUpperInvariant()}", settings);
            image.Draw(new DrawableComposite(30, 120, CompositeOperator.Over, globalTextImage));
            using var serverTextImage = new MagickImage($"caption:{GetText(guild.Id, Localization.CommonServer).ToUpperInvariant()}", settings);
            image.Draw(new DrawableComposite(30, 210, CompositeOperator.Over, serverTextImage));

            settings.TextGravity = Gravity.Center;
            using var globalLevelTextImage = new MagickImage($"caption:{GetText(guild.Id, Localization.XpLvl, xpInfo.GlobalLevel).ToUpperInvariant()}", settings);
            image.Draw(new DrawableComposite(250 - (double) globalLevelTextImage.Width / 2, 120, CompositeOperator.Over, globalLevelTextImage));
            using var serverLevelTextImage = new MagickImage($"caption:{GetText(guild.Id, Localization.XpLvl, xpInfo.ServerLevel).ToUpperInvariant()}", settings);
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

            var globalCurrentXp = RiasUtilities.LevelXp(xpInfo.GlobalLevel, xpInfo.GlobalXp, XpThreshold);
            var globalNextLevelXp = (xpInfo.GlobalLevel + 1) * 30;
            
            var globalXpBarLength = (double) globalCurrentXp  / globalNextLevelXp * 440;
            image.Draw(new Drawables()
                .RoundRectangle(30, 150, 30 + globalXpBarLength, 160, 5, 5)
                .FillColor(xpInfo.Color));
            
            var serverCurrentXp = RiasUtilities.LevelXp(xpInfo.ServerLevel, xpInfo.ServerXp, XpThreshold);
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
        
        private async Task<XpInfo> GetXpInfo(CachedMember member)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.Users.FirstOrDefaultAsync(x => x.UserId == member.Id);
            var profileDb = await db.Profile.FirstOrDefaultAsync(x => x.UserId == member.Id);

            var serverXpList = (await db.GetOrderedListAsync<GuildUsersEntity, int>(x => x.GuildId == member.Guild.Id, y => y.Xp, true))
                .Where(x => member.Guild.GetMember(x.UserId) != null)
                .ToList();
            
            var userServerXp = serverXpList.FirstOrDefault(x => x.UserId == member.Id);
            
            var globalXp = userDb?.Xp ?? 0;
            var serverXp = userServerXp?.Xp ?? 0;
            return new XpInfo
            {
                GlobalXp = globalXp,
                GlobalLevel = RiasUtilities.XpToLevel(globalXp, XpThreshold),
                GlobalRank = userDb != null
                    ? (await db.Users.Select(x => x.Xp)
                        .OrderByDescending(y => y)
                        .ToListAsync())
                    .IndexOf(userDb.Xp) + 1
                    : 0,
                ServerXp = serverXp,
                ServerLevel = RiasUtilities.XpToLevel(serverXp, XpThreshold),
                ServerRank = userServerXp != null ? serverXpList.IndexOf(userServerXp) + 1 : 0,
                Color = profileDb?.Color != null ? new MagickColor($"{profileDb.Color}") : MagickColors.White
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