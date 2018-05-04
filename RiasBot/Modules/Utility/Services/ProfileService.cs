using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Discord;
using RiasBot.Extensions;
using System.Linq;
using RiasBot.Services.Database.Models;
using ImageMagick;

namespace RiasBot.Modules.Utility.Services
{
    public class ProfileService : IRService
    {
        private readonly DbService _db;

        public ProfileService(DbService db)
        {
            _db = db;
        }

        private readonly string DefaultProfileBackground = "https://i.imgur.com/CeazwG7.png";
        private readonly string DefaultProfileBio = "Nothing here, just dust.";

        public async Task<MemoryStream> GenerateProfileImage(IGuildUser user, IRole highestRole)
        {
            string heartDiamondPath = "/assets/images/heart_diamond.png";
            var http = new HttpClient();

            using (var img = new MagickImage(MagickColors.White, 500, 300))
            {
                try
                {
                    var accentColor = GetUserHighRoleColor(highestRole);
                    var profileInfo = GetProfieInfo(user.Id);
                    var profileSettings = GetProfileSettings(user.Id);
                    //Init
                    string avatarUrl = null;
                    try
                    {
                        avatarUrl = user.RealAvatarUrl();
                    }
                    catch
                    {
                        avatarUrl = user.DefaultAvatarUrl();
                    }
                    string nickname = user.Nickname;

                    string arialFont = Environment.CurrentDirectory + "/assets/fonts/ArialBold.ttf";
                    string aweryFont = Environment.CurrentDirectory + "/assets/fonts/Awery.ttf";
                    string meiryoFont = Environment.CurrentDirectory + "/assets/fonts/Meiryo.ttf";

                    //Background
                    try
                    {
                        using (var bg = await http.GetStreamAsync(profileSettings.BackgroundUrl))
                        using (var tempBg = new MagickImage(bg))
                        {
                            MagickGeometry size = new MagickGeometry(img.Width, img.Height)
                            {
                                IgnoreAspectRatio = false,
                                FillArea = true
                            };
                            tempBg.Resize(size);

                            img.Draw(new DrawableComposite(0, 0, tempBg));
                        }
                    }
                    catch
                    {
                        using (var bg = await http.GetStreamAsync(DefaultProfileBackground))
                        using (var tempBg = new MagickImage(bg))
                        {
                            MagickGeometry size = new MagickGeometry(img.Width, img.Height)
                            {
                                IgnoreAspectRatio = false,
                                FillArea = true
                            };
                            tempBg.Resize(size);

                            img.Draw(new DrawableComposite(0, 0, tempBg));
                        }
                    }
                    float dim = ((float)profileSettings.BackgroundDim / 100) * 255;
                    img.Draw(new Drawables().FillColor(MagickColor.FromRgba(0, 0, 0, (byte)dim)).Rectangle(0, 0, 500, 300));
                    //Avatar
                    using (var temp = await http.GetStreamAsync(avatarUrl))
                    using (var tempDraw = new MagickImage(temp))
                    {
                        MagickGeometry size = new MagickGeometry(70, 70)
                        {
                            IgnoreAspectRatio = false,
                        };
                        tempDraw.Resize(size);
                        tempDraw.Border(2);
                        tempDraw.BorderColor = MagickColors.White;
                        img.Draw(new DrawableComposite(30, 20, tempDraw));
                    }
                    int usernameYPosition = (!String.IsNullOrEmpty(nickname)) ? 20 : 40;
                    var usernameSettings = new MagickReadSettings()
                    {
                        BackgroundColor = MagickColors.Transparent,
                        FillColor = MagickColors.White,
                        Font = meiryoFont,
                        Width = 150,
                        Height = 40
                    };

                    using (var username = new MagickImage("caption:" + user.ToString(), usernameSettings))
                    {
                        img.Draw(new DrawableComposite(120, usernameYPosition, username));
                    }
                    if (!String.IsNullOrEmpty(nickname))
                    {
                        var nicknameSettings = new MagickReadSettings()
                        {
                            BackgroundColor = MagickColors.Transparent,
                            FillColor = MagickColors.White,
                            Font = meiryoFont,
                            Width = 150,
                            Height = 40
                        };

                        using (var nicknameWrap = new MagickImage("caption:" + nickname, nicknameSettings))
                        {
                            img.Draw(new DrawableComposite(130, 60, nicknameWrap));
                        }
                    }
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(15, 120, 127, 284));
                    if (profileInfo.Waifus != null)
                    {
                        img.Draw(new Drawables().FontPointSize(17)
                        .Font(aweryFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Center)
                        .Text(70, 115, "Top Waifus"));

                        int x = 20;
                        bool nextH = false; //second waifu
                        int y = 125;
                        bool nextV = true; //fourth waifu
                        foreach (var waifu in profileInfo.Waifus.Take(4))
                        {
                            using (var waifuStream = await http.GetStreamAsync(waifu.WaifuPicture))
                            using (var tempWaifu = new MagickImage(waifuStream))
                            {
                                MagickGeometry size = new MagickGeometry(46, 72)
                                {
                                    IgnoreAspectRatio = false,
                                };
                                tempWaifu.Resize(size);
                                img.Draw(new DrawableComposite(x, y, tempWaifu));
                            }
                            if (!nextH)
                            {
                                x += 56;
                                nextH = true;
                            }
                            else
                            {
                                x = 20;
                                nextH = false;
                            }
                            if (!nextV)
                            {
                                y += 82;
                                nextV = true;
                            }
                            else
                            {
                                nextV = false;
                            }
                        }
                    }
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(137, 100, 365, 285));
                    
                    if (profileInfo.BelovedWaifu != null)
                    {
                        var belovedWaifuSettings = new MagickReadSettings()
                        {
                            BackgroundColor = MagickColors.Transparent,
                            FillColor = MagickColors.White,
                            Font = aweryFont,
                            TextGravity = Gravity.Center,
                            Width = 110,
                            Height = 20
                        };

                        using (var belovedWaifu = new MagickImage("caption:" + profileInfo.BelovedWaifu.WaifuName, belovedWaifuSettings))
                        {
                            img.Draw(new DrawableComposite(375, 100, belovedWaifu));
                        }

                        string waifuPicture = profileInfo.BelovedWaifu.WaifuPicture;
                        if (!String.IsNullOrEmpty(profileInfo.BelovedWaifu.BelovedWaifuPicture))
                            waifuPicture = profileInfo.BelovedWaifu.BelovedWaifuPicture;

                        try
                        {
                            using (var belovedWaifu = await http.GetStreamAsync(waifuPicture))
                            using (var tempBelovedWaifu = new MagickImage(belovedWaifu))
                            {
                                MagickGeometry size = new MagickGeometry(100, 155)
                                {
                                    IgnoreAspectRatio = false,
                                    FillArea = true
                                };
                                tempBelovedWaifu.Resize(size);
                                tempBelovedWaifu.Crop(100, 155);
                                img.Draw(new DrawableComposite(380, 125, tempBelovedWaifu));
                            }
                        }
                        catch
                        {
                            try
                            {
                                using (var belovedWaifu = await http.GetStreamAsync(profileInfo.BelovedWaifu.WaifuPicture))
                                using (var tempBelovedWaifu = new MagickImage(belovedWaifu))
                                {
                                    MagickGeometry size = new MagickGeometry(100, 155)
                                    {
                                        IgnoreAspectRatio = false,
                                    };
                                    tempBelovedWaifu.Resize(size);
                                    img.Draw(new DrawableComposite(380, 125, tempBelovedWaifu));
                                }
                            }
                            catch
                            {
                                // the image does not exists
                            }
                        }
                    }
                    else
                    {
                        img.Draw(new Drawables().FontPointSize(17)
                        .Font(aweryFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Center)
                        .Text(430, 110, "Beloved Waifu"));
                    }
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(375, 120, 485, 285));

                    // XP bar
                    img.Draw(new Drawables().StrokeWidth(1).StrokeColor(accentColor).FillColor(MagickColors.Transparent).Rectangle(147, 110, 355, 130));

                    int globalCurrentXp = profileInfo.GlobalXp;
                    int globalRequiredXp = 0;
                    while (globalCurrentXp >= 0)
                    {
                        globalRequiredXp += 30;
                        globalCurrentXp -= globalRequiredXp;
                    }
                    img.Draw(new Drawables().FillColor(accentColor).Rectangle(147, 110, 147 + (208 * ((globalCurrentXp + globalRequiredXp) / (float)globalRequiredXp)), 130));
                    img.Draw(new Drawables().FontPointSize(12)
                        .Font(arialFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Left)
                        .Text(147, 145, (globalCurrentXp + globalRequiredXp).ToString()));
                    img.Draw(new Drawables().FontPointSize(12)
                        .Font(arialFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Right)
                        .Text(355, 145, globalRequiredXp.ToString()));

                    // Global Level
                    img.Draw(new Drawables().FontPointSize(18)
                        .Font(aweryFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Center)
                        .Text(170, 165, "Level"));
                    img.Draw(new Drawables().FontPointSize(25)
                        .Font(arialFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Center)
                        .Text(170, 195, profileInfo.GlobalLevel.ToString()));

                    // Heart Diamonds
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(aweryFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Left)
                        .Text(210, 165, "Currency"));
                    using (var tempHeartDiamond = new MagickImage(Environment.CurrentDirectory + heartDiamondPath))
                    {
                        MagickGeometry size = new MagickGeometry(20, 20)
                        {
                            IgnoreAspectRatio = false,
                        };
                        tempHeartDiamond.Resize(size);
                        img.Draw(new DrawableComposite(345, 153, tempHeartDiamond));
                    }
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(arialFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Right)
                        .Text(343, 165, profileInfo.Currency.ToString()));
                    // Total XP
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(aweryFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Left)
                        .Text(210, 180, "Total XP"));
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(arialFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Right)
                        .Text(343, 180, profileInfo.GlobalXp.ToString()));
                    // Rank
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(aweryFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Left)
                        .Text(210, 195, "Rank"));
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(arialFont, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Right)
                        .Text(343, 195, "#" + profileInfo.Rank.ToString()));

                    // Bio
                    img.Draw(new Drawables().RoundRectangle(147, 207, 355, 275, 5, 5)
                        .FillColor(MagickColors.White));
                    var bioBoxsettings = new MagickReadSettings()
                    {
                        BackgroundColor = MagickColors.Transparent,
                        FillColor = MagickColors.Black,
                        Font = meiryoFont,
                        FontPointsize = 9,
                        Width = 198
                    };

                    string caption = $"caption:{profileInfo.Bio}";

                    using (var image = new MagickImage(caption, bioBoxsettings))
                    {
                        img.Draw(new DrawableComposite(152, 212, image));
                    }

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

        public ProfileInfo GetProfieInfo(ulong userID)
        {
            using (var db = _db.GetDbContext())
            {
                var profileInfo = new ProfileInfo();
                var waifus = db.Waifus.Where(x => x.UserId == userID);

                if (waifus != null)
                {
                    profileInfo.Waifus = waifus.Except(waifus.Where(x => x.IsPrimary == true)).ToList();
                    var belovedWaifu = waifus.Where(x => x.IsPrimary == true).FirstOrDefault();

                    if (belovedWaifu != null)
                    {
                        profileInfo.BelovedWaifu = belovedWaifu;
                    }
                }
                var userDb = db.Users.Where(x => x.UserId == userID).FirstOrDefault();
                if (userDb != null)
                {
                    var globalRanks = db.Users.OrderByDescending(x => x.Xp).Select(y => y.Xp).ToList();
                    int globalRank = globalRanks.IndexOf(userDb?.Xp ?? -1) + 1;

                    profileInfo.Currency = userDb.Currency;
                    profileInfo.GlobalXp = userDb.Xp;
                    profileInfo.GlobalLevel = userDb.Level;
                    profileInfo.Rank = globalRank;
                }
                var profileDb = db.Profile.Where(x => x.UserId == userID).FirstOrDefault();
                if (profileDb != null)
                {
                    profileInfo.MarriedUser = profileDb.MarriedUser;

                    if (!String.IsNullOrEmpty(profileDb.Bio))
                        profileInfo.Bio = profileDb.Bio;
                    else
                        profileInfo.Bio = DefaultProfileBio;
                }
                else
                {
                    profileInfo.Bio = DefaultProfileBio;
                }
                return profileInfo;
            }
        }

        public ProfileSettings GetProfileSettings(ulong userID)
        {
            using (var db = _db.GetDbContext())
            {
                var profileSettings = new ProfileSettings();
                var profileDb = db.Profile.Where(x => x.UserId == userID).FirstOrDefault();
                if (profileDb != null)
                {
                    if (!String.IsNullOrEmpty(profileDb.BackgroundUrl))
                        profileSettings.BackgroundUrl = profileDb.BackgroundUrl;
                    else
                        profileSettings.BackgroundUrl = DefaultProfileBackground;

                    profileSettings.BackgroundDim = profileDb.BackgroundDim;
                }
                else
                {
                    profileSettings.BackgroundUrl = DefaultProfileBackground;
                    profileSettings.BackgroundDim = 50;
                }
                return profileSettings;
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
                    return MagickColor.FromRgb(255, 255, 255);
                }
            }
            else
            {
                return MagickColor.FromRgb(255, 255, 255);
            }
        }
    }

    public class ProfileInfo
    {
        public int Currency { get; set; }
        public int GlobalXp { get; set; }
        public int GlobalLevel { get; set; }
        public int Rank { get; set; }
        public Waifus BelovedWaifu { get; set; }
        public List<Waifus> Waifus { get; set; }
        public ulong MarriedUser { get; set; }
        public string Bio { get; set; }
    }

    public class ProfileSettings
    {
        public string BackgroundUrl { get; set; }
        public int BackgroundDim { get; set; }
    }
}
