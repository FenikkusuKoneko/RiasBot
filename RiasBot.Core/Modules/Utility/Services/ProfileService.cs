using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Discord;
using System.Linq;
using RiasBot.Services.Database.Models;
using ImageMagick;
using RiasBot.Extensions;
using RiasBot.Modules.Searches.Services;

namespace RiasBot.Modules.Utility.Services
{
    public class ProfileService : IRService
    {
        private readonly AnimeService _animeService;
        private readonly DbService _db;

        public ProfileService(AnimeService animeService, DbService db)
        {
            _animeService = animeService;
            _db = db;
        }

        private const string DefaultProfileBackground = "https://i.imgur.com/CeazwG7.png";
        private const string DefaultProfileBio = "Nothing here, just dust.";
        
        private readonly string _diamondHeartPath = Path.Combine(Environment.CurrentDirectory, "assets/images/diamond_heart.png");
        private readonly string _arialFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/ArialBold.ttf");
        private readonly string _aweryFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/Awery.ttf");
        private readonly string _meiryoFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/Meiryo.ttf");

        public async Task<MemoryStream> GenerateBackgroundPreview(IGuildUser user, string url)
        {
            using (var http = new HttpClient())
            using (var img = new MagickImage(MagickColors.White, 500, 300))
            {
                try
                {
                    //Init
                    var avatarUrl = user.GetRealAvatarUrl();
                    var nickname = user.Nickname;

                    //Background
                    using (var bg = await http.GetAsync(url))
                    {
                        if (bg.IsSuccessStatusCode)
                        {
                            try
                            {
                                using (var tempBg = new MagickImage(await bg.Content.ReadAsStreamAsync()))
                                {
                                    var size = new MagickGeometry(img.Width, img.Height)
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
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    img.Draw(new Drawables().FillColor(MagickColor.FromRgba(0, 0, 0, 127)).Rectangle(0, 0, 500, 300));
                    //Avatar
                    using (var temp = await http.GetStreamAsync(avatarUrl))
                    using (var tempDraw = new MagickImage(temp))
                    {
                        var size = new MagickGeometry(70, 70)
                        {
                            IgnoreAspectRatio = false,
                        };
                        tempDraw.Resize(size);
                        tempDraw.Border(2);
                        tempDraw.BorderColor = MagickColors.White;
                        img.Draw(new DrawableComposite(30, 20, tempDraw));
                    }
                    var usernameYPosition = (!String.IsNullOrEmpty(nickname)) ? 20 : 40;
                    var usernameSettings = new MagickReadSettings()
                    {
                        BackgroundColor = MagickColors.Transparent,
                        FillColor = MagickColors.White,
                        Font = _meiryoFontPath,
                        Width = 150,
                        Height = 40
                    };

                    using (var username = new MagickImage("caption:" + user, usernameSettings))
                    {
                        img.Draw(new DrawableComposite(120, usernameYPosition, username));
                    }
                    if (!string.IsNullOrEmpty(nickname))
                    {
                        var nicknameSettings = new MagickReadSettings()
                        {
                            BackgroundColor = MagickColors.Transparent,
                            FillColor = MagickColors.White,
                            Font = _meiryoFontPath,
                            Width = 150,
                            Height = 40
                        };

                        using (var nicknameWrap = new MagickImage("caption:" + nickname, nicknameSettings))
                        {
                            img.Draw(new DrawableComposite(130, 60, nicknameWrap));
                        }
                    }

                    //Waifus
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(15, 120, 127, 284));
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(137, 100, 365, 285));
                    img.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(375, 120, 485, 285));

                    img.Draw(new Drawables().StrokeWidth(1).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(147, 110, 355, 130));

                    // Global Level
                    img.Draw(new Drawables().FontPointSize(18)
                        .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Center)
                        .Text(170, 165, "Level"));

                    // Heart Diamonds
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Left)
                        .Text(210, 165, "Currency"));
                    
                    // Total XP
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Left)
                        .Text(210, 180, "Total XP"));
                    // Rank
                    img.Draw(new Drawables().FontPointSize(13)
                        .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                        .FillColor(MagickColors.White)
                        .TextAlignment(TextAlignment.Left)
                        .Text(210, 195, "Rank"));

                    // Bio
                    img.Draw(new Drawables().RoundRectangle(147, 207, 355, 275, 5, 5)
                        .FillColor(MagickColors.White));

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

        public async Task<MemoryStream> GenerateProfileImageAsync(IGuildUser user, IRole highestRole)
        {
            using (var http = new HttpClient())
            using (var image = new MagickImage(MagickColors.White, 500, 300))
            {
                http.Timeout = TimeSpan.FromSeconds(10);
                
                //Init
                var accentColor = GetUserHighRoleColor(highestRole);
                var profileInfo = GetProfileInfo(user);
                
                //Background
                await AddBackgroundAsync(http, user, image).ConfigureAwait(false);
                
                //Avatar
                await AddAvatarAsync(http, user, image).ConfigureAwait(false);
                
                //Username & Nickname
                AddUsernameAndNickname(user, image);
                
                //Waifus box
                image.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(15, 120, 127, 284));
                //Info box
                image.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(375, 120, 485, 285));
                //Beloved waifu box
                image.Draw(new Drawables().StrokeWidth(2).StrokeColor(MagickColors.White).FillColor(MagickColors.Transparent).Rectangle(137, 100, 365, 285));
                
                //XP bar
                image.Draw(new Drawables().StrokeWidth(1).StrokeColor(accentColor).FillColor(MagickColors.Transparent).Rectangle(147, 110, 355, 130));
                
                //AddInfo + Fill XP bar
                AddInfo(profileInfo, image, accentColor);
                
                //Waifus
                await AddWaifusAsync(http, profileInfo, user, image).ConfigureAwait(false);
                
                //Beloved waifu
                await AddBelovedWaifuAsync(http, profileInfo, user, image).ConfigureAwait(false);
                
                //Write
                var imageStream = new MemoryStream();
                image.Write(imageStream, MagickFormat.Png);
                imageStream.Position = 0;
                return imageStream;
            }
        }

        private async Task AddBackgroundAsync(HttpClient http, IGuildUser user, IMagickImage image)
        {
            var profileSettings = GetProfileSettings(user);
            var addDefaultBackground = false;
            
            try
            {
                using (var bg = await http.GetAsync(profileSettings.BackgroundUrl).ConfigureAwait(false))
                {
                    if (bg.IsSuccessStatusCode)
                    {
                        using (var tempBg = new MagickImage(await bg.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                        {
                            var size = new MagickGeometry(image.Width, image.Height)
                            {
                                IgnoreAspectRatio = false,
                                FillArea = true
                            };
                            
                            tempBg.Resize(size);
                            image.Draw(new DrawableComposite(0, 0, tempBg));
                        }
                    }
                    else
                    {
                        addDefaultBackground = true;
                    }
                }
            }
            catch
            {
                addDefaultBackground = true;
            }

            if (addDefaultBackground)
            {
                try
                {
                    using (var defaultBg = await http.GetAsync(DefaultProfileBackground).ConfigureAwait(false))
                    {
                        if (defaultBg.IsSuccessStatusCode)
                        {
                            using (var tempBg = new MagickImage(await defaultBg.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                            {
                                var size = new MagickGeometry(image.Width, image.Height)
                                {
                                    IgnoreAspectRatio = false,
                                    FillArea = true
                                };
                                tempBg.Resize(size);

                                image.Draw(new DrawableComposite(0, 0, tempBg));
                            }
                        }
                    }
                }
                catch
                {
                    //ignored
                }
            }
            
            //Background dim
            var dim = ((float)profileSettings.BackgroundDim / 100) * 255;
            image.Draw(new Drawables().FillColor(MagickColor.FromRgba(0, 0, 0, (byte)dim)).Rectangle(0, 0, 500, 300));
        }

        private async Task AddAvatarAsync(HttpClient http, IGuildUser user, IMagickImage image)
        {
            using (var avatarStream = await http.GetStreamAsync(user.GetRealAvatarUrl()).ConfigureAwait(false))
            using (var tempAvatar = new MagickImage(avatarStream))
            {
                var size = new MagickGeometry(70, 70)
                {
                    IgnoreAspectRatio = false,
                };
                tempAvatar.Resize(size);
                tempAvatar.Border(2);
                tempAvatar.BorderColor = MagickColors.White;
                image.Draw(new DrawableComposite(30, 20, tempAvatar));
            }
        }

        private void AddUsernameAndNickname(IGuildUser user, IMagickImage image)
        {
            var nickname = user.Nickname;
            
            var nameSettings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.White,
                Font = _meiryoFontPath,
                Width = 150,
                Height = 40
            };

            var usernameYPosition = !string.IsNullOrEmpty(nickname) ? 20 : 40;
            using (var tempUsername = new MagickImage($"caption:{user}", nameSettings))
            {
                image.Draw(new DrawableComposite(120, usernameYPosition, tempUsername));
            }
            if (!string.IsNullOrEmpty(nickname))
            {
                using (var tempNickname = new MagickImage($"caption:{nickname}", nameSettings))
                {
                    image.Draw(new DrawableComposite(130, 60, tempNickname));
                }
            }
        }

        private void AddInfo(ProfileInfo profileInfo, IMagickImage image, MagickColor accentColor)
        {
            var globalLevel = profileInfo.GlobalLevel;
            var globalCurrentXp = profileInfo.GlobalXp - (30 + globalLevel * 30) * globalLevel / 2;
            var globalNextLevelXp = (globalLevel + 1) * 30;
            
            
            image.Draw(new Drawables().FillColor(accentColor).Rectangle(147, 110,
                147 + 208 * ((double)globalCurrentXp / globalNextLevelXp), 130));
            
            //XP texts
            image.Draw(new Drawables().FontPointSize(12)
                .Font(_arialFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Left)
                .Text(147, 145, globalCurrentXp.ToString()));
            image.Draw(new Drawables().FontPointSize(12)
                .Font(_arialFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Right)
                .Text(355, 145, globalNextLevelXp.ToString()));
            
            // Global Level
            image.Draw(new Drawables().FontPointSize(18)
                .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Center)
                .Text(170, 165, "Level"));
            image.Draw(new Drawables().FontPointSize(25)
                .Font(_arialFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Center)
                .Text(170, 195, profileInfo.GlobalLevel.ToString()));

            // Heart Diamonds
            image.Draw(new Drawables().FontPointSize(13)
                .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Left)
                .Text(210, 165, "Currency"));
            using (var tempHeartDiamond = new MagickImage(_diamondHeartPath))
            {
                var size = new MagickGeometry(20, 20)
                {
                    IgnoreAspectRatio = false,
                };
                tempHeartDiamond.Resize(size);
                image.Draw(new DrawableComposite(345, 153, tempHeartDiamond));
            }
            image.Draw(new Drawables().FontPointSize(13)
                .Font(_arialFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Right)
                .Text(343, 165, profileInfo.Currency.ToString()));
            
            // Total XP
            image.Draw(new Drawables().FontPointSize(13)
                .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Left)
                .Text(210, 180, "Total XP"));
            image.Draw(new Drawables().FontPointSize(13)
                .Font(_arialFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Right)
                .Text(343, 180, profileInfo.GlobalXp.ToString()));
            // Rank
            image.Draw(new Drawables().FontPointSize(13)
                .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Left)
                .Text(210, 195, "Rank"));
            image.Draw(new Drawables().FontPointSize(13)
                .Font(_arialFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Right)
                .Text(343, 195, $"#{profileInfo.Rank}"));
            
            // Bio
            image.Draw(new Drawables().RoundRectangle(147, 207, 355, 275, 5, 5)
                .FillColor(MagickColors.White));
            var bioBoxSettings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.Black,
                Font = _meiryoFontPath,
                FontPointsize = 9,
                Width = 198
            };

            var caption = $"caption:{profileInfo.Bio}";

            using (var tempBioBox = new MagickImage(caption, bioBoxSettings))
            {
                image.Draw(new DrawableComposite(152, 212, tempBioBox));
            }
        }

        private async Task AddWaifusAsync(HttpClient http, ProfileInfo profileInfo, IGuildUser user, IMagickImage image)
        {
            image.Draw(new Drawables().FontPointSize(17)
                .Font(_aweryFontPath, FontStyleType.Normal, FontWeight.Normal, FontStretch.Normal)
                .FillColor(MagickColors.White)
                .TextAlignment(TextAlignment.Center)
                .Text(70, 115, "Top Waifus"));
            
            var x = 20;
            var nextH = false; //second waifu
            var y = 125;
            var nextV = true; //fourth waifu

            var waifus = profileInfo.Waifus;
            if (!waifus.Any()) return;
            
            foreach (var waifu in waifus.OrderBy(w => w.Id).Take(4))
            {
                //if the waifu image url is not working
                //replace the old CDN url from AniList with the new one
                var getWaifuPicture = false;
                try
                {
                    using (var waifuStream = await http.GetAsync(waifu.WaifuPicture).ConfigureAwait(false))
                    {
                        if (waifuStream.IsSuccessStatusCode)
                        {
                            using (var tempWaifu = new MagickImage(await waifuStream.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                            {
                                var size = new MagickGeometry(46, 72)
                                {
                                    IgnoreAspectRatio = false,
                                };
                                tempWaifu.Resize(size);
                                image.Draw(new DrawableComposite(x, y, tempWaifu));
                            }
                        }
                        else
                        {
                            getWaifuPicture = true;
                        }
                    }
                }
                catch
                {
                    getWaifuPicture = true;
                }
                
                if (getWaifuPicture)
                {
                    //if it's a custom waifu
                    if (waifu.WaifuId == 0) return;
                    
                    var obj = await _animeService.CharacterSearch(waifu.WaifuId);
                    waifu.WaifuPicture = (string)obj.image.large;
                    await Task.Run(async () => await SaveNewWaifuPicture(user, waifu)).ConfigureAwait(false);

                    using (var waifuStreamUpdate = await http.GetAsync(waifu.WaifuPicture))
                    {
                        if (waifuStreamUpdate.IsSuccessStatusCode)
                        {
                            using (var tempWaifu = new MagickImage(await waifuStreamUpdate.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                            {
                                var size = new MagickGeometry(46, 72)
                                {
                                    IgnoreAspectRatio = false,
                                    FillArea = true
                                };
                                tempWaifu.Resize(size);
                                image.Draw(new DrawableComposite(x, y, tempWaifu));
                            }
                        }
                    }
                }
                
                //Change the X and Y position to draw the next waifu
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

        private async Task SaveNewWaifuPicture(IGuildUser user, Waifus waifu)
        {
            using (var db = _db.GetDbContext())
            {
                var getWaifu = db.Waifus.FirstOrDefault(w => w.UserId == user.Id && w.WaifuId == waifu.WaifuId);
                if (getWaifu != null)
                {
                    getWaifu.WaifuPicture = waifu.WaifuPicture;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task AddBelovedWaifuAsync(HttpClient http, ProfileInfo profileInfo, IGuildUser user, IMagickImage image)
        {
            if (profileInfo.BelovedWaifu != null)
            {
                var belovedWaifuSettings = new MagickReadSettings()
                {
                    BackgroundColor = MagickColors.Transparent,
                    FillColor = MagickColors.White,
                    Font = _aweryFontPath,
                    TextGravity = Gravity.Center,
                    Width = 110,
                    Height = 20
                };
                
                using (var tempBelovedWaifuText = new MagickImage("caption:" + profileInfo.BelovedWaifu.WaifuName, belovedWaifuSettings))
                {
                    image.Draw(new DrawableComposite(375, 100, tempBelovedWaifuText));
                }
                
                //if the custom waifu image cannot be downloaded then add the waifu image
                var addWaifuPicture = false;
                
                //if the waifu image url is not working
                //replace the old CDN url from AniList with the new one
                var getWaifuPicture = false;

                var waifuPicture = profileInfo.BelovedWaifu.BelovedWaifuPicture;
                if (string.IsNullOrEmpty(waifuPicture))
                {
                    waifuPicture = profileInfo.BelovedWaifu.WaifuPicture;
                    getWaifuPicture = true;
                }

                try
                {
                    using (var belovedWaifu = await http.GetAsync(waifuPicture).ConfigureAwait(false))
                    {
                        if (belovedWaifu.IsSuccessStatusCode)
                        {
                            using (var tempBelovedWaifu = new MagickImage(await belovedWaifu.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                            {
                                var size = new MagickGeometry(100, 155)
                                {
                                    IgnoreAspectRatio = false,
                                    FillArea = true
                                };
                                tempBelovedWaifu.Resize(size);
                                tempBelovedWaifu.Crop(100, 155);
                                image.Draw(new DrawableComposite(380, 125, tempBelovedWaifu));
                            }
                        }
                        else
                        {
                            addWaifuPicture = true;
                        }
                    }
                }
                catch
                {
                    addWaifuPicture = true;
                }

                if (addWaifuPicture && !getWaifuPicture)
                {
                    try
                    {
                        using (var belovedWaifu = await http.GetAsync(profileInfo.BelovedWaifu.WaifuPicture).ConfigureAwait(false))
                        {
                            if (belovedWaifu.IsSuccessStatusCode)
                            {
                                using (var tempBelovedWaifu = new MagickImage(await belovedWaifu.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                                {
                                    var size = new MagickGeometry(100, 155)
                                    {
                                        IgnoreAspectRatio = false,
                                        FillArea = true
                                    };
                                    tempBelovedWaifu.Resize(size);
                                    tempBelovedWaifu.Crop(100, 155);
                                    image.Draw(new DrawableComposite(380, 125, tempBelovedWaifu));
                                }
                            }
                            else
                            {
                                getWaifuPicture = true;
                            }
                        }
                    }
                    catch
                    {
                        getWaifuPicture = true;
                    }
                }

                if (getWaifuPicture)
                {
                    var waifu = profileInfo.BelovedWaifu;
                    //if it's a custom waifu
                    if (waifu.WaifuId == 0) return;
                    
                    var obj = await _animeService.CharacterSearch(waifu.WaifuId);
                    waifu.WaifuPicture = (string)obj.image.large;
                    await Task.Run(async () => await SaveNewWaifuPicture(user, waifu)).ConfigureAwait(false);
                    
                    try
                    {
                        using (var belovedWaifu = await http.GetAsync(waifu.WaifuPicture).ConfigureAwait(false))
                        {
                            if (belovedWaifu.IsSuccessStatusCode)
                            {
                                using (var tempBelovedWaifu = new MagickImage(await belovedWaifu.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                                {
                                    var size = new MagickGeometry(100, 155)
                                    {
                                        IgnoreAspectRatio = false,
                                        FillArea = true
                                    };
                                    tempBelovedWaifu.Resize(size);
                                    tempBelovedWaifu.Crop(100, 155);
                                    image.Draw(new DrawableComposite(380, 125, tempBelovedWaifu));
                                }
                            }
                        }
                    }
                    catch
                    {
                        //ignored
                    }
                }
            }
        }

        private ProfileInfo GetProfileInfo(IGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var profileInfo = new ProfileInfo();
                var waifus = db.Waifus.Where(x => x.UserId == user.Id);
                 
                if (waifus.Any())
                {
                    profileInfo.Waifus = waifus.Except(waifus.Where(waifu => waifu.IsPrimary)).ToList();
                    profileInfo.BelovedWaifu = waifus.FirstOrDefault(waifu => waifu.IsPrimary);
                }
                
                var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
                if (userDb != null)
                {
                    var globalRank = db.Users.OrderByDescending(x => x.Xp).ToList().IndexOf(userDb) + 1;

                    profileInfo.Currency = userDb.Currency;
                    profileInfo.GlobalXp = userDb.Xp;
                    profileInfo.GlobalLevel = userDb.Level;
                    profileInfo.Rank = globalRank;
                }
                
                var profileDb = db.Profile.FirstOrDefault(x => x.UserId == user.Id);
                if (profileDb != null)
                {
                    profileInfo.Bio = !string.IsNullOrEmpty(profileDb.Bio) ? profileDb.Bio : DefaultProfileBio;
                }
                else
                {
                    profileInfo.Bio = DefaultProfileBio;
                }
                
                return profileInfo;
            }
        }

        private ProfileSettings GetProfileSettings(IGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var profileSettings = new ProfileSettings();
                var profileDb = db.Profile.FirstOrDefault(x => x.UserId == user.Id);
                if (profileDb != null)
                {
                    profileSettings.BackgroundUrl = !string.IsNullOrEmpty(profileDb.BackgroundUrl) ? profileDb.BackgroundUrl : DefaultProfileBackground;
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

        private static MagickColor GetUserHighRoleColor(IRole role)
        {
            if (string.Equals(role.Name, "@everyone") || role.Color.Equals(Color.Default))
                return MagickColor.FromRgb(255, 255, 255);

            return MagickColor.FromRgb(role.Color.R, role.Color.G, role.Color.B);
        }
    }

    public class ProfileInfo
    {
        public int Currency { get; set; }
        public int GlobalXp { get; set; }
        public int GlobalLevel { get; set; }
        public int Rank { get; set; }
        public Waifus BelovedWaifu { get; set; }
        public IList<Waifus> Waifus { get; set; }
        public string Bio { get; set; }
    }

    public class ProfileSettings
    {
        public string BackgroundUrl { get; set; }
        public int BackgroundDim { get; set; }
    }
}
