using Discord;
using ImageSharp;
using Image = ImageSharp.Image;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RiasBot.Extensions;
using System.Net.Http;
using SixLabors.Primitives;
using ImageSharp.Drawing;
using SixLabors.Fonts;

namespace RiasBot.Modules.Xp.Services
{
    public class XpService : IKService
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
                int currentXp = 0;
                int currentLevel = 0;
                int levelXp = 0;
                int nextLevel = 0;
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

        public async Task XpUserGuildMessage(IGuild guild, IGuildUser user, IMessageChannel channel)
        {
            using (var db = _db.GetDbContext())
            {
                var guildXp = db.XpSystem.Where(x => x.GuildId == guild.Id);
                var xpDb = guildXp.Where(x => x.UserId == user.Id).FirstOrDefault();

                int currentXp = 0;
                int currentLevel = 0;
                int levelXp = 0;
                int nextLevel = 0;
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
                    bool xpNotify = false;
                    try
                    {
                        xpNotify = guildDb.XpGuildNotification;
                    }
                    catch
                    {
                    }
                    if (xpNotify)
                        await channel.SendConfirmationEmbed($"Congratulations {user.Mention}, you've reached server level {nextLevel - 1}").ConfigureAwait(false);
                }
            }
        }

        public async Task<MemoryStream> GenerateXpImage(IGuildUser user, (int, int) level, (int, int) currentXp, (int, int) requiredXp, int globalRank, int guildRank, IRole highestRole)
        {
            string kurumiPath = "/assets/images/xp/xp_model.png";
            string xpWhitePattern = "/assets/images/xp/xp_white_pattern.png";
            string xpBlackPattern = "/assets/images/xp/xp_black_pattern.png";
            string globalXpBarBgPath = "/assets/images/xp/global_xp_bar_bg.png";
            string guildXpBarBgPath = "/assets/images/xp/guild_xp_bar_bg.png";
            var http = new HttpClient();

            (int globalLevel, int guildLevel) = level;
            (int globalCurrentXp, int guildCurrentXp) = currentXp;
            (int globalRequiredXp, int guildRequiredXp) = requiredXp;

            using (var img = Image.Load(Environment.CurrentDirectory + kurumiPath))
            using (var whitePattern = Image.Load(Environment.CurrentDirectory + xpWhitePattern))
            using (var blackPattern = Image.Load(Environment.CurrentDirectory + xpBlackPattern))
            using (var globalXpBarBg = Image.Load(Environment.CurrentDirectory + globalXpBarBgPath))
            using (var guildXpBarBg = Image.Load(Environment.CurrentDirectory + guildXpBarBgPath))
            {
                try
                {
                    //Init
                    var avatarUrl = user.RealAvatarUrl() ?? user.DefaultAvatarUrl();
                    int usernameSize = (user.ToString().Length < 15) ? 25 : 25 - user.ToString().Length / 5;
                    FontCollection fonts = new FontCollection();
                    FontFamily whitneyBold = fonts.Install(Environment.CurrentDirectory + "/assets/fonts/WhitneyBold.ttf");
                    FontFamily arialFont = fonts.Install(Environment.CurrentDirectory + "/assets/fonts/ArialBold.ttf");
                    var roleColor = GetUserHighRoleColor(highestRole);

                    var foreColor = (ImageExtension.PerceivedBrightness(roleColor) > 130 ? Rgba32.Black : Rgba32.White);

                    //Pattern
                    img.FillPolygon(roleColor, new PointF[]
                        {
                            new PointF(0, 0),
                            new PointF(img.Width, 0),
                            new PointF(img.Width, img.Height),
                            new PointF(0, img.Height)
                        });
                    if (foreColor == Rgba32.White)
                    {
                        img.DrawImage(whitePattern, 1, new Size(img.Width, img.Height), new Point(0, 0));
                    }
                    else
                    {
                        img.DrawImage(blackPattern, 1, new Size(img.Width, img.Height), new Point(0, 0));
                    }
                    //Avatar
                    img.Fill(foreColor, ImageExtension.AvatarStroke(250, 45, 35));

                    using (var temp = await http.GetStreamAsync(avatarUrl))
                    using (var tempDraw = Image.Load(temp).Resize(70, 70))
                    {
                        tempDraw.Round(35);
                        img.DrawImage(tempDraw,
                            1,
                            new Size(70, 70),
                            new Point(215, 10)
                    );
                    }
                    img.DrawPolygon(foreColor, 2, new PointF[]
                        {
                            new PointF(10, 130),
                            new PointF(115, 130),
                            new PointF(115, 205),
                            new PointF(10, 205)
                        });
                    img.DrawPolygon(foreColor, 2, new PointF[]
                        {
                            new PointF(10, 215),
                            new PointF(115, 215),
                            new PointF(115, 290),
                            new PointF(10, 290)
                        });

                    img.DrawImage(globalXpBarBg, 1, new Size(globalXpBarBg.Width, globalXpBarBg.Height), new Point(125, 130));
                    img.DrawImage(guildXpBarBg, 1, new Size(guildXpBarBg.Width, guildXpBarBg.Height), new Point(125, 215));

                    var xpBgColor = new Rgba32(roleColor.R, roleColor.G, roleColor.B, 150);

                    //Username, GlobalLevel
                    img.DrawText(user.ToString(), new Font(whitneyBold, usernameSize), foreColor, new PointF(250, 100), new TextGraphicsOptions()
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                    img.DrawText($"GLOBAL", new Font(whitneyBold, 17), foreColor, new PointF(60, 140), new TextGraphicsOptions()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    img.DrawText($"SERVER", new Font(whitneyBold, 17), foreColor, new PointF(60, 223), new TextGraphicsOptions()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    img.DrawText($"LVL. {globalLevel}", new Font(arialFont, 15), foreColor, new PointF(60, 160), new TextGraphicsOptions()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    img.DrawText($"LVL. {guildLevel}", new Font(arialFont, 15), foreColor, new PointF(60, 243), new TextGraphicsOptions()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    img.DrawText($"#{globalRank}", new Font(arialFont, 15), foreColor, new PointF(60, 180), new TextGraphicsOptions()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    img.DrawText($"#{guildRank}", new Font(arialFont, 15), foreColor, new PointF(60, 265), new TextGraphicsOptions()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    //GlobalLevel
                    img.FillPolygon(xpBgColor, new[]
                    {
                        new PointF(125, 130),
                        new PointF(125 + (350 * (globalCurrentXp / (float)globalRequiredXp)), 130),
                        new PointF(125 + (350 * (globalCurrentXp / (float)globalRequiredXp)), 205),
                        new PointF(125, 205),
                    });
                    //GuildLevel
                    img.FillPolygon(xpBgColor, new[]
                    {
                        new PointF(125, 215),
                        new PointF(125 + (350 * (guildCurrentXp / (float)guildRequiredXp)), 215),
                        new PointF(125 + (350 * (guildCurrentXp / (float)guildRequiredXp)), 290),
                        new PointF(125, 290),
                    });

                    var imageData = new MemoryStream();
                    img.SaveAsPng(imageData);
                    imageData.Position = 0;

                    img.DrawText($"{globalCurrentXp}/{globalRequiredXp}", new Font(whitneyBold, 17), Rgba32.Black, new PointF(300, 165), new TextGraphicsOptions(true)
                    {
                        BlenderMode = ImageSharp.PixelFormats.PixelBlenderMode.Src,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    img.DrawText($"{guildCurrentXp}/{guildRequiredXp}", new Font(whitneyBold, 17), Rgba32.Black, new PointF(300, 245), new TextGraphicsOptions(true)
                    {
                        BlenderMode = ImageSharp.PixelFormats.PixelBlenderMode.Src,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    var imageStream = new MemoryStream();
                    img.SaveAsPng(imageStream);
                    imageStream.Position = 0;
                    return imageStream;
                }
                catch
                {
                    return null;
                }
            }
        }

        private Rgba32 GetUserHighRoleColor(IRole role)
        {
            if (role.Name != "@everyone")
            {
                if (!role.Color.Equals(Color.Default))
                {
                    var r = role.Color.R;
                    var g = role.Color.G;
                    var b = role.Color.B;

                    return new Rgba32(r, g, b);
                }
                else
                {
                    return Rgba32.White;
                }
            }
            else
            {
                return Rgba32.White;
            }
        }
    }
}
