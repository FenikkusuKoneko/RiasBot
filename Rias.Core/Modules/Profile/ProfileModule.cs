using System;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Profile
{
    [Name("Profile")]
    public class ProfileModule : RiasModule<ProfileService>
    {
        private readonly HttpClient _httpClient;
        public ProfileModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }
        
        [Command, Context(ContextType.Guild),
         Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task ProfileAsync(CachedMember? member = null)
        {
            member ??= (CachedMember) Context.User;
            using var _ = Context.Channel.Typing();

            await using var profileImage = await Service.GenerateProfileImageAsync(member);
            await Context.Channel.SendMessageAsync(new[] {new LocalAttachment(profileImage, $"{member.Id}_profile.png")});
        }
        
        [Command("background"), Context(ContextType.Guild),
         Cooldown(1, 30, CooldownMeasure.Seconds, BucketType.User)]
        public async Task BackgroundAsync(string url)
        {
            var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UsersEntity {UserId = Context.User.Id});
            if (userDb.Currency < 1000)
            {
                await ReplyErrorAsync(Localization.GamblingCurrencyNotEnough, Credentials.Currency);
                return;
            }
            
            if (!Uri.TryCreate(url, UriKind.Absolute, out var backgroundUri))
            {
                await ReplyErrorAsync(Localization.UtilityUrlNotValid);
                Context.Command.ResetCooldowns();
                return;
            }
            
            if (backgroundUri.Scheme != Uri.UriSchemeHttps)
            {
                await ReplyErrorAsync(Localization.UtilityUrlNotHttps);
                Context.Command.ResetCooldowns();
                return;
            }
            
            using var _ = Context.Channel.Typing();
            
            using var result = await _httpClient.GetAsync(backgroundUri);
            if (!result.IsSuccessStatusCode)
            {
                await ReplyErrorAsync(Localization.UtilityImageOrUrlNotGood);
                Context.Command.ResetCooldowns();
                return;
            }

            await using var backgroundStream = await result.Content.ReadAsStreamAsync();
            if (!(RiasUtilities.IsPng(backgroundStream) || RiasUtilities.IsJpg(backgroundStream)))
            {
                await ReplyErrorAsync(Localization.UtilityUrlNotPngJpg);
                return;
            }

            backgroundStream.Position = 0;
            await using var profilePreview = await Service.GenerateProfileBackgroundAsync(Context.User, backgroundStream);
            await Context.Channel.SendMessageAsync(new[] {new LocalAttachment(profilePreview, $"{Context.User.Id}_profile_preview.png")}, GetText(Localization.ProfileBackgroundPreview));
            
            var messageReceived = await NextMessageAsync();
            if (!string.Equals(messageReceived?.Message.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync(Localization.ProfileBackgroundCanceled);
                return;
            }

            userDb.Currency -= 1000;
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity {UserId = Context.User.Id, BackgroundDim = 50});
            profileDb.BackgroundUrl = url;
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileBackgroundSet);
        }
        
        [Command("dim"), Context(ContextType.Guild)]
        public async Task DimAsync(int dim)
        {
            if (dim < 0 || dim > 100)
            {
                await ReplyErrorAsync(Localization.ProfileBackgroundDimBetween, 0, 100);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity {UserId = Context.User.Id});
            profileDb.BackgroundDim = dim;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileBackgroundSet, dim);
        }
        
        [Command("biography"), Context(ContextType.Guild)]
        public async Task BiographyAsync([Remainder] string bio)
        {
            if (bio.Length > 200)
            {
                await ReplyErrorAsync(Localization.ProfileBiographyLimit, 200);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity {UserId = Context.User.Id, BackgroundDim = 50});
            profileDb.Biography = bio;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileBiographySet);
        }
        
        [Command("color"), Context(ContextType.Guild)]
        public async Task ColorAsync([Remainder] Color color)
        {
            if (!await Service.CheckColorAsync(Context.User, color))
            {
                await ReplyErrorAsync(Localization.ProfileInvalidColor, PatreonService.ProfileColorTier, Credentials.Patreon);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity {UserId = Context.User.Id, BackgroundDim = 50});
            profileDb.Color = color.ToString();

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileColorSet);
        }
        
        [Command("badge"), Context(ContextType.Guild)]
        public async Task BadgeAsync(int index, [Remainder] string? text = null)
        {
            index--;
            if (index < 0)
                index = 0;

            if (Credentials.PatreonConfig != null && Context.User.Id != Credentials.MasterId)
            {
                var patreonTier = (await DbContext.Patreon.FirstOrDefaultAsync(x => x.UserId == Context.User.Id))?.Tier ?? 0;
                switch (index)
                {
                    case 0 when patreonTier < PatreonService.ProfileFirstBadgeTier:
                        await ReplyErrorAsync(Localization.ProfileFirstBadgeNoPatreon, PatreonService.ProfileFirstBadgeTier, Credentials.Patreon);
                        return;
                    case 1 when patreonTier < PatreonService.ProfileSecondBadgeTier:
                        await ReplyErrorAsync(Localization.ProfileSecondBadgeNoPatreon, PatreonService.ProfileSecondBadgeTier, Credentials.Patreon);
                        return;
                    case 2 when patreonTier < PatreonService.ProfileThirdBadgeTier:
                        await ReplyErrorAsync(Localization.ProfileThirdBadgeNoPatreon, PatreonService.ProfileThirdBadgeTier, Credentials.Patreon);
                        return;
                }
            }

            if (index > 2)
            {
                await ReplyErrorAsync(Localization.ProfileBadgeError);
                return;
            }

            if (string.Equals(text, "master", StringComparison.InvariantCultureIgnoreCase) && Context.User.Id != Credentials.MasterId)
            {
                await ReplyErrorAsync(Localization.ProfileBadgeNotAvailable);
                return;
            }

            if (!string.IsNullOrEmpty(text) && text.Length > 20)
            {
                await ReplyErrorAsync(Localization.ProfileBadgeTextLimit);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id,
                () => new ProfileEntity {UserId = Context.User.Id, BackgroundDim = 50});
            profileDb.Badges ??= new string[3];
                
            profileDb.Badges[index] = text!;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(string.IsNullOrEmpty(text) ? Localization.ProfileBadgeRemoved : Localization.ProfileBadgeSet);
        }
    }
}