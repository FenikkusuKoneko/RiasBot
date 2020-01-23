using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.WebSocket;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Color = Discord.Color;

namespace Rias.Core.Services
{
    public class ProfileService : RiasService
    {
        private readonly HttpClient _httpClient;
        private readonly AnimeService _animeService;
        
        public ProfileService(IServiceProvider services) : base(services)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _animeService = services.GetRequiredService<AnimeService>();
        }
        
        private readonly string _defaultBackgroundPath = Path.Combine(Environment.CurrentDirectory, "assets/images/default_background.png");
        private const string DefaultBiography = "Nothing here, just dust.";

        private readonly MagickColor _dark = MagickColor.FromRgb(36, 36, 36);
        private readonly MagickColor _darker = MagickColor.FromRgb(32, 32, 32);
        
        private readonly string _arialFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/ArialBold.ttf");
        private readonly string _meiryoFontPath = Path.Combine(Environment.CurrentDirectory, "assets/fonts/Meiryo.ttf");

        private readonly Color[] _colors =
        {
            new Color(255, 255, 255),    //white
            new Color(255, 0, 0),        //red
            new Color(0, 255, 0),        //green
            new Color(0, 255, 255),      //cyan/aqua
            new Color(255, 165, 0),      //orange
            new Color(255, 105, 180),    //hot pink
            new Color(255, 0, 255)       //magenta
        };

        public async Task<Stream> GenerateProfileImageAsync(SocketGuildUser user)
        {
            var profileInfo = await GetProfileInfoAsync(user);

            var height = profileInfo.Waifus!.Count == 0 && profileInfo.SpecialWaifu is null ? 500 : 750;
            using var image = new MagickImage(_dark, 500, height);

            await AddBackgroundAsync(image, profileInfo);
            await AddAvatarAndUsernameAsync(image, user, profileInfo);
            AddInfo(image, profileInfo, user.Guild);

            if (profileInfo.Waifus!.Count != 0 || profileInfo.SpecialWaifu != null)
                await AddWaifusAsync(image, profileInfo);
            
            var imageStream = new MemoryStream();
            image.Write(imageStream, MagickFormat.Png);
            imageStream.Position = 0;
            return imageStream;
        }

        public async Task<Stream> GenerateProfileBackgroundAsync(SocketUser user, Stream backgroundStream)
        {
            using var image = new MagickImage(_dark, 500, 500);
            
            using var tempBackgroundImage = new MagickImage(backgroundStream);
            tempBackgroundImage.Resize(new MagickGeometry
            {
                Width = 500,
                Height = 250,
                IgnoreAspectRatio = false,
                FillArea = true
            });
                
            using var backgroundImageLayer = new MagickImage(MagickColors.Black, 500, 250);
            backgroundImageLayer.Draw(new DrawableComposite(0, 0, CompositeOperator.Over, tempBackgroundImage));
            
            image.Draw(new DrawableComposite(0, 0, backgroundImageLayer));
            image.Draw(new Drawables().Rectangle(0, 0, 500, 250).FillColor(MagickColor.FromRgba(0, 0, 0, 128)));
            
            await AddAvatarAndUsernameAsync(image, user);
            
            var imageStream = new MemoryStream();
            image.Write(imageStream, MagickFormat.Png);
            imageStream.Position = 0;
            return imageStream;
        }

        public async Task<bool> CheckColorAsync(SocketUser user, Color color)
        {
            if (Creds.PatreonConfig is null)
                return true;
            
            if (user.Id == Creds.MasterId)
                return true;

            if (_colors.Any(x => x == color))
                return true;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var tier = (await db.Patreon.FirstOrDefaultAsync(x => x.UserId == user.Id))?.Tier ?? 0;
            return tier >= 2;
        }

        private bool CheckColorAsync(SocketUser user, Color color, int tier)
        {
            if (user.Id == Creds.MasterId)
                return true;

            if (_colors.Any(x => x == color))
                return true;

            return tier >= 2;
        }

        private async Task AddBackgroundAsync(MagickImage image, ProfileInfo profileInfo)
        {
            if (string.IsNullOrEmpty(profileInfo.BackgroundUrl))
            {
                AddBackground(null, image, profileInfo);
                return;
            }
            
            try
            {
                using var response = await _httpClient.GetAsync(profileInfo.BackgroundUrl);
                if (!response.IsSuccessStatusCode)
                {
                    AddBackground(null, image, profileInfo);
                    return;
                }
                
                await using var backgroundStream = await response.Content.ReadAsStreamAsync();
                
                if (!(RiasUtils.IsPng(backgroundStream) || RiasUtils.IsJpg(backgroundStream)))
                {
                    AddBackground(null, image, profileInfo);
                    return;
                }

                backgroundStream.Position = 0;
                using var tempBackgroundImage = new MagickImage(backgroundStream);

                tempBackgroundImage.Resize(new MagickGeometry
                {
                    Width = 500,
                    Height = 250,
                    IgnoreAspectRatio = false,
                    FillArea = true
                });
                
                using var backgroundImageLayer = new MagickImage(MagickColors.Black, 500, 250);
                backgroundImageLayer.Draw(new DrawableComposite(0, 0, CompositeOperator.Over, tempBackgroundImage));
                AddBackground(backgroundImageLayer, image, profileInfo);
            }
            catch
            {
                AddBackground(null, image, profileInfo);
            }
        }

        private void AddBackground(MagickImage? backgroundImage, MagickImage image, ProfileInfo profileInfo)
        {
            using var background = backgroundImage ?? new MagickImage(_defaultBackgroundPath);
            var backgroundDrawable = new DrawableComposite
            (
                (double) (background.Width - 500) / 2,
                (double) (background.Height - 250) / 2,
                background
            );
            image.Draw(backgroundDrawable);
            
            var dim = (float) profileInfo.Dim / 100 * 255;
            image.Draw(new Drawables().Rectangle(0, 0, 500, 250).FillColor(MagickColor.FromRgba(0, 0, 0, (byte) dim)));
        }

        private async Task AddAvatarAndUsernameAsync(MagickImage image, SocketUser user, ProfileInfo? profileInfo = null)
        {
            await using var avatarStream = await _httpClient.GetStreamAsync(user.GetRealAvatarUrl());
            using var avatarImage = new MagickImage(avatarStream);
            avatarImage.Resize(new MagickGeometry
            {
                Width = 100,
                Height = 100
            });
            
            using var avatarLayer = new MagickImage(MagickColors.Transparent, 100, 100);
            avatarLayer.Draw(new Drawables().RoundRectangle(0, 0, avatarLayer.Width, avatarLayer.Height, 15, 15).FillColor(MagickColors.White));
            avatarImage.Composite(avatarLayer, CompositeOperator.DstIn);
            avatarLayer.Draw(new DrawableComposite(0, 0, CompositeOperator.Over, avatarImage));
            
            image.Draw(new DrawableComposite(30, 120, CompositeOperator.Over, avatarLayer));
            
            var usernameSettings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.White,
                Font = _meiryoFontPath,
                FontWeight = FontWeight.Bold,
                Width = 250,
                Height = 50
            };
            
            using var usernameImage = new MagickImage($"caption:{user}", usernameSettings);
            image.Draw(new DrawableComposite(150, 150, CompositeOperator.Over, usernameImage));

            if (profileInfo is null)
                return;
            
            var badgeSettings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.White,
                Font = _arialFontPath,
                FontPointsize = 15,
            };

            if (profileInfo.PatreonTier == 0 && user.Id != Creds.MasterId)
                return;

            var badges = new List<string>();
            if (user.Id == Creds.MasterId)
            {
                if (profileInfo.Badges != null && profileInfo.Badges.Count != 0)
                    badges.AddRange(profileInfo.Badges);
                else
                    badges.Add("Master");
            }
            else if (profileInfo.PatreonTier < 3)
            {
                badges.Add("Supporter");
            }
            else if (profileInfo.Badges != null && profileInfo.Badges.Count != 0)
            {
                if (profileInfo.Badges.Count != 0)
                    badges.Add(profileInfo.Badges[0]);
                if (profileInfo.PatreonTier >= 5 && profileInfo.Badges.Count > 1)
                    badges.Add(profileInfo.Badges[1]);
                if (profileInfo.PatreonTier >= 6 && profileInfo.Badges.Count > 2)
                    badges.Add(profileInfo.Badges[2]);
            }
            else
            {
                badges.Add("Supporter");
            }

            var x = 150.0;
            foreach (var badge in badges)
                AddBadge(image, profileInfo, badgeSettings, badge, ref x);
        }

        private void AddBadge(MagickImage image, ProfileInfo profileInfo, MagickReadSettings badgeSettings, string badge, ref double x)
        {
            using var badgeImage = new MagickImage($"caption:{badge}", badgeSettings);
            var badgeWidth = badgeImage.Width + badgeImage.Width * 30 / 100;
            
            using var badgeLayer = new MagickImage(MagickColors.Transparent, badgeWidth, 20);
            badgeLayer.Draw(new Drawables()
                .RoundRectangle(0, 0, badgeLayer.Width, badgeLayer.Height, 10, 10)
                .FillColor(profileInfo.Color));
            badgeLayer.Composite(badgeImage, Gravity.Center, CompositeOperator.DstOut);
            
            image.Draw(new DrawableComposite(x, 200, CompositeOperator.Over, badgeLayer));
            x += badgeWidth + 10;
        }

        private void AddInfo(MagickImage image, ProfileInfo profileInfo, SocketGuild guild)
        {
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = profileInfo.Color,
                Font = _arialFontPath,
                FontPointsize = 20,
                Width = 125,
                TextGravity = Gravity.Center
            };
            
            using var currencyImage = new MagickImage($"caption:{profileInfo.Currency}", settings);
            image.Draw(new DrawableComposite(100 - (double) currencyImage.Width / 2, 280, CompositeOperator.Over, currencyImage));
            
            using var levelImage = new MagickImage($"caption:{profileInfo.Level}", settings);
            image.Draw(new DrawableComposite(200 - (double) levelImage.Width / 2, 280, CompositeOperator.Over, levelImage));
            
            using var totalXpImage = new MagickImage($"caption:{profileInfo.Xp}", settings);
            image.Draw(new DrawableComposite(300 - (double) totalXpImage.Width / 2, 280, CompositeOperator.Over, totalXpImage));
            
            using var rankImage = new MagickImage($"caption:{profileInfo.Rank}", settings);
            image.Draw(new DrawableComposite(400 - (double) rankImage.Width / 2, 280, CompositeOperator.Over, rankImage));
            
            settings.FillColor = MagickColors.White;
            settings.FontPointsize = 15;

#if GLOBAL || DEBUG
            const string currency = "Hearts";
#else
            const string currency = "Currency";
#endif
            
            using var currencyTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Gambling", currency)}", settings);
            image.Draw(new DrawableComposite(100 - (double) currencyTextImage.Width / 2, 315, CompositeOperator.Over, currencyTextImage));
            
            using var levelTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Xp", "Level")}", settings);
            image.Draw(new DrawableComposite(200 - (double) levelTextImage.Width / 2, 315, CompositeOperator.Over, levelTextImage));
            
            using var totalXpTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Xp", "TotalXp")}", settings);
            image.Draw(new DrawableComposite(300 - (double) totalXpTextImage.Width / 2, 315, CompositeOperator.Over, totalXpTextImage));
            
            using var rankTextImage = new MagickImage($"caption:{Resources.GetText(guild.Id, "Common", "Rank")}", settings);
            image.Draw(new DrawableComposite(400 - (double) rankTextImage.Width / 2, 315, CompositeOperator.Over, rankTextImage));
            
            image.Draw(new Drawables()
                .RoundRectangle(50, 360, 450, 370, 5, 5)
                .FillColor(_darker));

            var currentXp = RiasUtils.LevelXp(profileInfo.Level, profileInfo.Xp, XpService.XpThreshold);
            var nextLevelXp = (profileInfo.Level + 1) * 30;
            
            var xpBarLength = (double) currentXp  / nextLevelXp * 400;
            image.Draw(new Drawables()
                .RoundRectangle(50, 360, 50 + xpBarLength, 370, 5, 5)
                .FillColor(profileInfo.Color));

            settings.Width = 0;
            using var xpImage = new MagickImage($"caption:{currentXp}", settings);
            image.Draw(new DrawableComposite(50, 380, CompositeOperator.Over, xpImage));
            
            using var nextLevelXpImage = new MagickImage($"caption:{nextLevelXp}", settings);
            image.Draw(new DrawableComposite(450 - nextLevelXpImage.Width, 380, CompositeOperator.Over, nextLevelXpImage));

            image.Draw(new Drawables()
                .RoundRectangle(25, 415, 475, 495, 10, 10)
                .FillColor(_darker));

            settings.Font = _meiryoFontPath;
            settings.FontPointsize = 12;
            settings.TextGravity = Gravity.West;
            settings.Width = 440;
            
            using var bioImage = new MagickImage($"caption:{profileInfo.Biography}", settings);
            image.Draw(new DrawableComposite(30, 420, CompositeOperator.Over, bioImage));
        }

        private async Task AddWaifusAsync(MagickImage image, ProfileInfo profileInfo)
        {
            var waifuSize = new MagickGeometry
            {
                Width = 100,
                Height = 150,
                IgnoreAspectRatio = false,
                FillArea = true
            };
            
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                FillColor = MagickColors.White,
                Font = _arialFontPath,
                FontPointsize = 15,
                Width = 150,
                TextGravity = Gravity.Center
            };

            var waifuY = 530;
            
            if (profileInfo.Waifus!.Count != 0)
                await AddWaifu(image, profileInfo.Waifus![0], new Point(100, waifuY), waifuSize, settings);

            if (profileInfo.SpecialWaifu is null)
            {
                if (profileInfo.Waifus!.Count > 1)
                    await AddWaifu(image, profileInfo.Waifus[1], new Point(250, waifuY), waifuSize, settings);
                if (profileInfo.Waifus!.Count > 2)
                    await AddWaifu(image, profileInfo.Waifus[2], new Point(400, waifuY), waifuSize, settings);
            }
            else
            {
                if (profileInfo.Waifus!.Count > 1)
                    await AddWaifu(image, profileInfo.Waifus[1], new Point(400, waifuY), waifuSize, settings);
                
                settings.FillColor = profileInfo.Color;
                await AddWaifu(image, profileInfo.SpecialWaifu, new Point(250, waifuY), waifuSize, settings);
            }
        }

        private async Task AddWaifu(MagickImage image, IWaifus waifu, Point position, MagickGeometry waifuSize, MagickReadSettings settings)
        {
            await using var waifuStream = await GetWaifuStreamAsync(waifu, waifu.IsSpecial);
            if (waifuStream != null)
            {
                using var waifuImage = new MagickImage(waifuStream);
                waifuImage.Resize(waifuSize);

                var waifuX = position.X - waifuSize.Width / 2;
                using var waifuImageLayer = new MagickImage(MagickColors.Black, waifuSize.Width, waifuSize.Height);
                var waifuDrawable = new DrawableComposite
                (
                    (double) (waifuSize.Width - waifuImage.Width) / 2,
                    (double) (waifuSize.Height - waifuImage.Height) / 2,
                    CompositeOperator.Over,
                    waifuImage
                );
                waifuImageLayer.Draw(waifuDrawable);
                
                image.Draw(new DrawableComposite(waifuX, position.Y, waifuImageLayer));
                image.Draw(new Drawables()
                    .RoundRectangle(waifuX - 1, position.Y - 1, waifuX + waifuSize.Width + 1, position.Y + waifuSize.Height + 1, 5, 5)
                    .StrokeWidth(2)
                    .StrokeColor(settings.FillColor)
                    .FillColor(MagickColors.Transparent));
            }
            
            using var waifuNameImage = new MagickImage($"caption:{waifu.Name}", settings);
            image.Draw(new DrawableComposite(position.X - waifuNameImage.Width / 2, 690, CompositeOperator.Over, waifuNameImage));
        }

        private async Task<Stream?> GetWaifuStreamAsync(IWaifus waifu, bool useCustomImage = false)
        {
            try
            {
                var imageUrl = waifu.ImageUrl;
                if (useCustomImage && waifu is Waifus waifus && !string.IsNullOrEmpty(waifus.CustomImageUrl))
                    imageUrl = waifus.CustomImageUrl;

                using var response = await _httpClient.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                {
                    await using var waifuStream = await response.Content.ReadAsStreamAsync();
                    if ((RiasUtils.IsPng(waifuStream) || RiasUtils.IsJpg(waifuStream)))
                    {
                        waifuStream.Position = 0;
                        var ms = new MemoryStream();
                        await waifuStream.CopyToAsync(ms);
                        ms.Position = 0;
                        return ms;
                    }
                }
            }
            catch
            {
                // ignored
            }

            if (!(waifu is Waifus waifuDb))
                return null;

            if (useCustomImage)
                return await GetWaifuStreamAsync(waifu);

            if (!waifuDb.CharacterId.HasValue)
                return null;

            var aniListCharacter = await _animeService.GetAniListCharacterById(waifuDb.CharacterId.Value);
            if (aniListCharacter is null)
                return null;

            var characterImage = aniListCharacter.Image.Large;
            if (!string.IsNullOrEmpty(characterImage))
            {
                await _animeService.SetCharacterImageUrlAsync(aniListCharacter.Id, characterImage);
                await using var characterStream = await _httpClient.GetStreamAsync(characterImage);
                var ms = new MemoryStream();
                await characterStream.CopyToAsync(ms);
                ms.Position = 0;
                return ms;
            }
                
            return null;
        }

        private async Task<ProfileInfo> GetProfileInfoAsync(SocketUser user)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
            var profileDb = await db.Profile.FirstOrDefaultAsync(x => x.UserId == user.Id);
            var patreonDb = Creds.PatreonConfig != null ? await db.Patreon.FirstOrDefaultAsync(x => x.UserId == user.Id) : null;

            var waifus = db.Waifus
                .Include(x => x.Character)
                .Include(x => x.CustomCharacter)
                .Where(x => x.UserId == user.Id)
                .AsEnumerable()
                .Cast<IWaifus>()
                .ToList();
            
            waifus.AddRange(db.CustomWaifus.Where(x => x.UserId == user.Id));

            var xp = userDb?.Xp ?? 0;

            var color = profileDb?.Color != null ? new Color(RiasUtils.HexToUint(profileDb.Color[1..]).GetValueOrDefault()) : _colors[0];
            if (color != _colors[0] && !CheckColorAsync(user, color, patreonDb?.Tier ?? 0))
                color = _colors[0];

            var specialWaifu = waifus.FirstOrDefault(x => x.IsSpecial);
            waifus.Remove(specialWaifu);
            
            return new ProfileInfo
            {
                Currency = userDb?.Currency ?? 0,
                Xp = xp,
                Level = RiasUtils.XpToLevel(xp, XpService.XpThreshold),
                Rank = userDb != null
                    ? (await db.Users.Select(x => x.Xp)
                          .OrderByDescending(y => y)
                          .ToListAsync())
                      .IndexOf(userDb.Xp) + 1
                    : 0,
                BackgroundUrl = profileDb?.BackgroundUrl,
                Dim = profileDb?.BackgroundDim ?? 50,
                Biography = profileDb?.Biography ?? DefaultBiography,
                Color = new MagickColor(color.ToString()),
                Badges = profileDb?.Badges,
                Waifus = waifus.Where(x => x.Position != 0)
                    .OrderBy(x => x.Position)
                    .Concat(waifus.Where(x=> x.Position == 0))
                    .ToList(),
                SpecialWaifu = specialWaifu,
                PatreonTier = patreonDb?.Tier ?? 0
            };
        }

        private class ProfileInfo
        {
            public int Currency { get; set; }
            public int Xp { get; set; }
            public int Level { get; set; }
            public int Rank { get; set; }
            public string? BackgroundUrl { get; set; }
            public int Dim { get; set; }
            public string? Biography { get; set; }
            public MagickColor? Color { get; set; }
            public IList<string>? Badges { get; set; }
            public IList<IWaifus>? Waifus { get; set; }
            public IWaifus? SpecialWaifu { get; set; }
            public int PatreonTier { get; set; }
        }
    }
}