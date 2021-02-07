using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.Entities;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Database;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Models;
using Serilog;

namespace Rias.Services
{
    public class XpService : RiasService
    {
        public const int XpThreshold = 30;
        
        private readonly BotService _botService;
        private readonly HttpClient _httpClient;
        
        private readonly ConcurrentDictionary<ulong, DateTime> _usersXp = new();
        private readonly ConcurrentDictionary<(ulong, ulong), DateTime> _guildUsersXp = new();
        private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> _xpIgnoredChannels = new();

        private readonly MagickColor _dark = MagickColor.FromRgb(36, 36, 36);
        private readonly MagickColor _darker = MagickColor.FromRgb(32, 32, 32);
        
        private readonly string _arialFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/ArialBold.ttf");
        private readonly string _meiryoFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/Meiryo.ttf");
        
        public XpService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _botService = serviceProvider.GetRequiredService<BotService>();
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();

            RunTaskAsync(LoadXpIgnoredChannels);
        }
        
        public static string ReplacePlaceholders(DiscordMember member, DiscordRole? role, int level, string message)
        {
            var username = member.FullName().Replace("\\", "\\\\").Replace("\"", "\\\"");
            var guildName = member.Guild.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var sb = new StringBuilder(message)
                .Replace("%mention%", member.Mention)
                .Replace("%member%", username)
                .Replace("%user%", username)
                .Replace("%guild%", guildName)
                .Replace("%server%", guildName)
                .Replace("%level%", level.ToString())
                .Replace("%avatar%", member.GetAvatarUrl(ImageFormat.Auto));

            if (role != null)
            {
                sb.Replace("%role%", role.Name.Replace("\\", "\\\\").Replace("\"", "\\\""))
                    .Replace("%role_mention%", role.Mention);
            }

            return sb.ToString();
        }

        public async Task AddUserXpAsync(DiscordUser user)
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
            var userDb = await db.GetOrAddAsync(x => x.UserId == user.Id, () => new UserEntity { UserId = user.Id, Xp = -5 });
            
            if (check && userDb.LastMessageDate + TimeSpan.FromMinutes(5) > now)
                return;
            
            userDb.Xp += 5;
            userDb.LastMessageDate = now;
            
            await db.SaveChangesAsync();
            _usersXp[user.Id] = now;
        }
        
        public async Task AddGuildUserXpAsync(DiscordMember member, DiscordChannel channel)
        {
            if (CheckExcludedChannel(channel))
                return;
            
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
            var memberDb = await db.GetOrAddAsync(
                x => x.GuildId == member.Guild.Id && x.MemberId == member.Id,
                () => new MembersEntity { GuildId = member.Guild.Id, MemberId = member.Id });
            
            if (check && memberDb.LastMessageDate + TimeSpan.FromMinutes(5) > now)
                return;
            
            var currentLevel = RiasUtilities.XpToLevel(memberDb.Xp, 30);
            memberDb.Xp += 5;
            memberDb.LastMessageDate = now;
            var nextLevel = RiasUtilities.XpToLevel(memberDb.Xp, 30);

            await db.SaveChangesAsync();
            _guildUsersXp[(member.Guild.Id, member.Id)] = now;

            if (currentLevel == nextLevel)
                return;
            
            var guild = member.Guild;
            DiscordRole? role = null;
            
            var currentMember = guild.CurrentMember;
            if (currentMember.GetPermissions().HasPermission(Permissions.ManageRoles))
            {
                var xpRoleDb = await db.GuildXpRoles.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.Level == nextLevel);
                if (xpRoleDb != null)
                {
                    role = guild.GetRole(xpRoleDb.RoleId);
                    if (role != null && member.Roles.All(x => x.Id != role.Id))
                        await member.GrantRoleAsync(role);
                }
            }
            
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
            if (guildDb != null && guildDb.XpNotification)
                await SendXpNotificationAsync(member, channel, role, guildDb, nextLevel);
        }

        public bool CheckExcludedChannel(DiscordChannel channel)
            => _xpIgnoredChannels.TryGetValue(channel.GuildId, out var xpIgnoredGuildChannels) && xpIgnoredGuildChannels.Contains(channel.Id);

        public async Task AddChannelToExclusionAsync(DiscordChannel channel)
        {
            var channelAdded = true;
            
            if (_xpIgnoredChannels.TryGetValue(channel.GuildId, out var xpIgnoredGuildChannels))
                channelAdded = xpIgnoredGuildChannels.Add(channel.Id);
            else
                _xpIgnoredChannels[channel.GuildId] = new ConcurrentHashSet<ulong> { channel.Id };

            if (!channelAdded)
                return;
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();

            var guildDb = await db.GetOrAddAsync(g => g.GuildId == channel.GuildId, () => new GuildEntity { GuildId = channel.GuildId });
            if (guildDb.XpIgnoredChannels is null)
                guildDb.XpIgnoredChannels = new [] { channel.Id };
            else if (!guildDb.XpIgnoredChannels.Contains(channel.Id))
                guildDb.XpIgnoredChannels = guildDb.XpIgnoredChannels.Append(channel.Id).ToArray();

            await db.SaveChangesAsync();
        }
        
        public async Task RemoveChannelFromExclusionAsync(DiscordChannel channel)
        {
            var channelRemoved = _xpIgnoredChannels.TryGetValue(channel.GuildId, out var xpIgnoredGuildChannels) && xpIgnoredGuildChannels.TryRemove(channel.Id);
            if (!channelRemoved)
                return;
            
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();

            var guildDb = await db.GetOrAddAsync(g => g.GuildId == channel.GuildId, () => new GuildEntity { GuildId = channel.GuildId });
            if (guildDb.XpIgnoredChannels is not null)
            {
                var newXpIgnoredChannels = new ulong[guildDb.XpIgnoredChannels.Length - 1];
                var index = 0;
                    
                foreach (var xpIgnoredChannel in guildDb.XpIgnoredChannels)
                {
                    if (xpIgnoredChannel != channel.Id)
                        newXpIgnoredChannels[index++] = xpIgnoredChannel;
                }
                
                guildDb.XpIgnoredChannels = newXpIgnoredChannels;
            }
            
            if (guildDb.XpIgnoredChannels?.Length == 0)
                guildDb.XpIgnoredChannels = null;

            await db.SaveChangesAsync();
        }

        public async Task<Stream> GenerateXpImageAsync(DiscordMember member)
        {
            var xpInfo = await GetXpInfo(member);

            using var image = new MagickImage(_dark, 500, 300);

            await AddAvatarAndUsernameAsync(image, member);
            AddInfo(image, xpInfo, member.Guild);

            var imageStream = new MemoryStream();
            await image.WriteAsync(imageStream, MagickFormat.Png);
            imageStream.Position = 0;
            return imageStream;
        }

        private async Task LoadXpIgnoredChannels()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildsDb = await db.Guilds.Where(g => g.XpIgnoredChannels != null).ToListAsync();
            foreach (var guildDb in guildsDb)
            {
                foreach (var xpIgnoredChannel in guildDb.XpIgnoredChannels!)
                {
                    if (_xpIgnoredChannels.TryGetValue(guildDb.GuildId, out var xpIgnoredGuildChannels))
                        xpIgnoredGuildChannels.Add(xpIgnoredChannel);
                    else
                        _xpIgnoredChannels[guildDb.GuildId] = new ConcurrentHashSet<ulong> { xpIgnoredChannel };
                }
            }
            
            Log.Debug("Xp ignored channels loaded.");
        }

        private async Task SendXpNotificationAsync(DiscordMember member, DiscordChannel channel, DiscordRole? role, GuildEntity guildDb, int level)
        {
            var guild = member.Guild;
            var currentMember = guild.CurrentMember;

            if (guildDb.XpWebhookId == 0)
            {
                if (!currentMember.PermissionsIn(channel).HasPermission(Permissions.SendMessages))
                    return;

                if (role is null)
                {
                    if (guildDb.XpLevelUpMessage is null)
                    {
                        if (!currentMember.PermissionsIn(channel).HasPermission(Permissions.EmbedLinks))
                            return;
                        
                        await ReplyConfirmationAsync(channel, guild.Id, Localization.XpGuildLevelUp, member.Mention, level);
                    }
                    else
                    {
                        var message = ReplacePlaceholders(member, role, level, guildDb.XpLevelUpMessage);
                        if (RiasUtilities.TryParseMessage(message, out var customMessage))
                        {
                            if (customMessage.Embed is not null && !currentMember.PermissionsIn(channel).HasPermission(Permissions.EmbedLinks))
                                return;
                            
                            await channel.SendMessageAsync(customMessage.Content, customMessage.Embed);
                        }
                        else
                        {
                            await channel.SendMessageAsync(message);
                        }
                    }
                }
                else
                {
                    if (guildDb.XpLevelUpRoleRewardMessage is null)
                    {
                        if (!currentMember.PermissionsIn(channel).HasPermission(Permissions.EmbedLinks))
                            return;
                        
                        await ReplyConfirmationAsync(channel, guild.Id, Localization.XpGuildLevelUpRoleReward, member.Mention, level, role.Name);
                    }
                    else
                    {
                        var message = ReplacePlaceholders(member, role, level, guildDb.XpLevelUpRoleRewardMessage);
                        if (RiasUtilities.TryParseMessage(message, out var customMessage))
                        {
                            if (customMessage.Embed is not null && !currentMember.PermissionsIn(channel).HasPermission(Permissions.EmbedLinks))
                                return;
                            
                            await channel.SendMessageAsync(customMessage.Content, customMessage.Embed);
                        }
                        else
                        {
                            await channel.SendMessageAsync(message);
                        }
                    }
                }
                
                return;
            }

            if (!currentMember.GetPermissions().HasPermission(Permissions.ManageWebhooks))
            {
                await DisableXpNotificationAsync(guild);
                return;
            }
            
            if (!_botService.Webhooks.TryGetValue(guild.Id, out var webhooks))
            {
                webhooks = new List<DiscordWebhook>();
                _botService.Webhooks.TryAdd(guild.Id, webhooks);
            }
            
            var webhook = webhooks.FirstOrDefault(x => x.Id == guildDb.XpWebhookId);
            if (webhook is null)
            {
                webhook = await guild.GetWebhookAsync(guildDb.XpWebhookId);
                if (webhook is null)
                {
                    await DisableXpNotificationAsync(guild);
                    return;
                }
                
                webhooks.Add(webhook);
            }

            string? webhookMessage = null;
            var webhookCustomMessage = new CustomMessage();
                
            if (role is null)
            {
                if (guildDb.XpLevelUpMessage is null)
                {
                    webhookCustomMessage.Embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Description = GetText(guild.Id, Localization.XpGuildLevelUp, member.Mention, level)
                    };
                }
                else
                {
                    webhookMessage = ReplacePlaceholders(member, role, level, guildDb.XpLevelUpMessage);
                    if (RiasUtilities.TryParseMessage(webhookMessage, out webhookCustomMessage))
                        webhookMessage = null;
                }
            }
            else
            {
                if (guildDb.XpLevelUpRoleRewardMessage is null)
                {
                    webhookCustomMessage.Embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Description = GetText(guild.Id, Localization.XpGuildLevelUpRoleReward, member.Mention, level, role)
                    };
                }
                else
                {
                    webhookMessage = ReplacePlaceholders(member, role, level, guildDb.XpLevelUpRoleRewardMessage);
                    if (RiasUtilities.TryParseMessage(webhookMessage, out webhookCustomMessage))
                        webhookMessage = null;
                }
            }

            if (webhookMessage is not null)
            {
                await webhook.ExecuteAsync(new DiscordWebhookBuilder
                {
                    Username = currentMember.Username,
                    AvatarUrl = currentMember.AvatarUrl,
                    Content = webhookMessage
                }.AddMention(new UserMention(member)));
            }
            else
            {
                await webhook.ExecuteAsync(new DiscordWebhookBuilder
                {
                    Username = currentMember.Username,
                    AvatarUrl = currentMember.AvatarUrl,
                    Content = webhookCustomMessage.Content
                }.AddEmbed(webhookCustomMessage.Embed).AddMention(new UserMention(member)));
            }
        }

        private async Task DisableXpNotificationAsync(DiscordGuild guild)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);
                    
            guildDb.XpNotification = false;
            await db.SaveChangesAsync();
        }

        private async Task AddAvatarAndUsernameAsync(MagickImage image, DiscordUser user)
        {
            await using var avatarStream = await _httpClient.GetStreamAsync(user.GetAvatarUrl(ImageFormat.Auto));
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
            
            using var usernameImage = new MagickImage($"caption:{user.FullName()}", usernameSettings);
            image.Draw(new DrawableComposite(120, 45, CompositeOperator.Over, usernameImage));
        }
        
        private void AddInfo(MagickImage image, XpInfo xpInfo, DiscordGuild guild)
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
            
            var globalXpBarLength = (double) globalCurrentXp / globalNextLevelXp * 440;
            image.Draw(new Drawables()
                .RoundRectangle(30, 150, 30 + globalXpBarLength, 160, 5, 5)
                .FillColor(xpInfo.Color));
            
            var serverCurrentXp = RiasUtilities.LevelXp(xpInfo.ServerLevel, xpInfo.ServerXp, XpThreshold);
            var serverNextLevelXp = (xpInfo.ServerLevel + 1) * 30;
            
            var serverXpBarLength = (double) serverCurrentXp / serverNextLevelXp * 440;
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
        
        private async Task<XpInfo> GetXpInfo(DiscordMember member)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.Users.FirstOrDefaultAsync(x => x.UserId == member.Id);
            var profileDb = await db.Profile.FirstOrDefaultAsync(x => x.UserId == member.Id);

            var membersXp = (await db.GetOrderedListAsync<MembersEntity, int>(x => x.GuildId == member.Guild.Id, y => y.Xp, true))
                .Where(x => member.Guild.Members.ContainsKey(x.MemberId))
                .ToList();
            
            var memberXp = membersXp.FirstOrDefault(x => x.MemberId == member.Id);
            var serverRank = "?";
            if (memberXp != null && RiasBot.ChunkedGuilds.Contains(member.Guild.Id))
                serverRank = (membersXp.IndexOf(memberXp) + 1).ToString();
            
            var globalXp = userDb?.Xp ?? 0;
            var serverXp = memberXp?.Xp ?? 0;
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
                ServerRank = serverRank,
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
            
            public string? ServerRank { get; set; }
            
            public MagickColor? Color { get; set; }
        }
    }
}