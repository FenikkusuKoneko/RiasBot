using Discord;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RiasBot.Extensions;
using System.Net.Http;
using ImageMagick;

namespace RiasBot.Modules.Xp.Services
{
    public class XpService : IRService
    {
        private readonly DbService _db;
        public XpService(DbService db)
        {
            _db = db;
        }

        public async Task XpUserMessage(IGuildUser user, IMessageChannel channel)
        {
            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                var currentXp = 0;
                var currentLevel = 0;
                var levelXp = 0;
                var nextLevel = 0;
                try
                {
                    currentXp = userDb.Xp;
                    currentLevel = userDb.Level;
                }
                catch
                {
                    var xp = new UserConfig { UserId = user.Id, Xp = 0, Level = 0, MessageDateTime = DateTime.UtcNow };
                    await db.Users.AddAsync(xp).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }

                var timeout = userDb.MessageDateTime;

                if (DateTime.UtcNow >= timeout.Add(new TimeSpan(0, 5, 0)))
                {
                    userDb.Xp += 5;
                    userDb.MessageDateTime = DateTime.UtcNow;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }

                while (currentXp >= 0)
                {
                    levelXp += 30;
                    currentXp -= levelXp;
                    nextLevel++;
                }

                if (nextLevel - 1 > currentLevel)
                {
                    userDb.Level = nextLevel - 1;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task XpUserGuildMessage(IGuild guild, IGuildUser user, IMessageChannel channel, bool sendXpNotificationMessage = false)
        {
            using (var db = _db.GetDbContext())
            {
                var guildXp = db.XpSystem.Where(x => x.GuildId == guild.Id);
                var xpDb = guildXp.Where(x => x.UserId == user.Id).FirstOrDefault();

                var currentXp = 0;
                var currentLevel = 0;
                var levelXp = 0;
                var nextLevel = 0;
                try
                {
                    currentXp = xpDb.Xp;
                    currentLevel = xpDb.Level;
                }
                catch
                {
                    var xp = new XpSystem { GuildId = guild.Id, UserId = user.Id, Xp = 0, Level = 0, MessageDateTime = DateTime.UtcNow };
                    await db.XpSystem.AddAsync(xp).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }

                var timeout = xpDb.MessageDateTime;

                if (DateTime.UtcNow >= timeout.Add(new TimeSpan(0, 5, 0)))
                {
                    xpDb.Xp += 5;
                    xpDb.MessageDateTime = DateTime.UtcNow;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }

                while (currentXp >= 0)
                {
                    levelXp += 30;
                    currentXp -= levelXp;
                    nextLevel++;
                }

                if (nextLevel - 1 > currentLevel)
                {
                    xpDb.Level = nextLevel - 1;
                    await db.SaveChangesAsync().ConfigureAwait(false);

                    var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                    var xpNotify = false;
                    try
                    {
                        xpNotify = guildDb.XpGuildNotification;
                    }
                    catch { }
                    
                    if (sendXpNotificationMessage)
                    {
                        if (xpNotify)
                            await channel.SendConfirmationMessageAsync($"Congratulations {user.Mention}, you've reached server level {nextLevel - 1}").ConfigureAwait(false);
                    }
                    await RoleRewardUser(guild, user, nextLevel - 1).ConfigureAwait(false);
                }
            }
        }

        public async Task RoleRewardUser(IGuild guild, IGuildUser user, int level)
        {
            using (var db = _db.GetDbContext())
            {
                var roleReward = db.XpRolesSystem.Where(x => x.GuildId == guild.Id).FirstOrDefault(y => y.Level == level);
                if (roleReward != null)
                {
                    var role = guild.GetRole(roleReward.RoleId);
                    if (role != null)
                    {
                        await user.AddRoleAsync(role).ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task<MemoryStream> GenerateXpImage(IGuildUser user, (int, int) level, (int, int) currentXp, (int, int) requiredXp, int globalRank, int guildRank, IRole highestRole)
        {
            var xpWhitePattern = "/assets/images/xp/xp_white_pattern.png";
            var xpBlackPattern = "/assets/images/xp/xp_black_pattern.png";
            var globalXpBarBgPath = "/assets/images/xp/global_xp_bar_bg.png";
            var guildXpBarBgPath = "/assets/images/xp/guild_xp_bar_bg.png";

            (var globalLevel, var guildLevel) = level;
            (var globalCurrentXp, var guildCurrentXp) = currentXp;
            (var globalRequiredXp, var guildRequiredXp) = requiredXp;

            var roleColor = GetUserHighRoleColor(highestRole);

            using (var http = new HttpClient())
            using (var img = new MagickImage(roleColor, 500, 300))
            using (var whitePattern = new MagickImage(Environment.CurrentDirectory + xpWhitePattern))
            using (var blackPattern = new MagickImage(Environment.CurrentDirectory + xpBlackPattern))
            using (var globalXpBarBg = new MagickImage(Environment.CurrentDirectory + globalXpBarBgPath))
            using (var guildXpBarBg = new MagickImage(Environment.CurrentDirectory + guildXpBarBgPath))
            {
                try
                {
                    //Init
                    var avatarUrl = user.GetRealAvatarUrl();
                    var aweryFont = Environment.CurrentDirectory + "/assets/fonts/Awery.ttf";
                    var meiryoFont = Environment.CurrentDirectory + "/assets/fonts/Meiryo.ttf";

                    var foreColor = (ImageExtension.PerceivedBrightness(roleColor) > 130) ? MagickColors.Black : MagickColors.White;

                    //Pattern
                    if (foreColor == MagickColors.White)
                    {
                        img.Draw(new DrawableComposite(0, 0, whitePattern));
                    }
                    else
                    {
                        img.Draw(new DrawableComposite(0, 0, blackPattern));
                    }
                    //Avatar
                    using (var avatar = await http.GetStreamAsync(avatarUrl))
                    using (var tempBg = new MagickImage(avatar))
                    {
                        var size = new MagickGeometry(70, 70)
                        {
                            IgnoreAspectRatio = false,
                            FillArea = true
                        };
                        tempBg.Resize(size);
                        tempBg.Roundify();
                        img.Draw(new DrawableComposite(215, 10, tempBg));
                    }
                    img.Draw(new Drawables().StrokeWidth(3).StrokeColor(foreColor).FillColor(MagickColors.Transparent).RoundRectangle(213, 8, 286, 81, 45, 45));
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(foreColor).FillColor(MagickColors.Transparent).Rectangle(10, 130, 115, 205));
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(foreColor).FillColor(MagickColors.Transparent).Rectangle(10, 215, 115, 290));

                    img.Draw(new DrawableComposite(125, 130, globalXpBarBg));
                    img.Draw(new DrawableComposite(125, 215, guildXpBarBg));

                    //Username, GlobalLevel
                    var usernameSettings = new MagickReadSettings()
                    {
                        BackgroundColor = MagickColors.Transparent,
                        FillColor = foreColor,
                        Font = meiryoFont,
                        Width = 400,
                        Height = 35,
                        TextGravity = Gravity.Center
                    };

                    using (var username = new MagickImage("caption:" + user.ToString(), usernameSettings))
                    {
                        img.Draw(new DrawableComposite(50, 90, username));
                    }

                    img.Draw(new Drawables().FillColor(foreColor).Text(60, 150, "GLOBAL").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(17));
                    img.Draw(new Drawables().FillColor(foreColor).Text(60, 234, "SERVER").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(17));
                    img.Draw(new Drawables().FillColor(foreColor).Text(60, 170, $"LVL. {globalLevel}").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(15));
                    img.Draw(new Drawables().FillColor(foreColor).Text(60, 254, $"LVL. {guildLevel}").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(15));
                    img.Draw(new Drawables().FillColor(foreColor).Text(60, 190, $"#{globalRank}").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(15));
                    img.Draw(new Drawables().FillColor(foreColor).Text(60, 276, $"#{guildRank}").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(15));

                    //GlobalLevel
                    var xpBgColor = MagickColor.FromRgba((byte)roleColor.R, (byte)roleColor.G, (byte)roleColor.B, 127);

                    img.Draw(new Drawables().FillColor(xpBgColor).Rectangle(125, 130, 125 + (350 * (globalCurrentXp / (float)globalRequiredXp)), 205));
                    img.Draw(new Drawables().FillColor(xpBgColor).Rectangle(125, 215, 125 + (350 * (guildCurrentXp / (float)guildRequiredXp)), 290));
                    img.Draw(new Drawables().FillColor(MagickColors.Black).Text(300, 175, $"{globalCurrentXp}/{globalRequiredXp}").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(17));
                    img.Draw(new Drawables().FillColor(MagickColors.Black).Text(300, 255, $"{guildCurrentXp}/{guildRequiredXp}").TextAlignment(TextAlignment.Center).Font(aweryFont).FontPointSize(17));
                    
                    var imageStream = new MemoryStream();
                    img.Write(imageStream, MagickFormat.Png);
                    imageStream.Position = 0;
                    return imageStream;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return null;
                }
            }
        }

        private MagickColor GetUserHighRoleColor(IRole role)
        {
            if (role.Name != "@everyone")
            {
                if (!role.Color.Equals(Color.Default))
                {
                    var r = role.Color.R;
                    var g = role.Color.G;
                    var b = role.Color.B;

                    return MagickColor.FromRgb(r, g, b);
                }
                else
                {
                    return MagickColors.White;
                }
            }
            else
            {
                return MagickColors.White;
            }
        }
    }
}
