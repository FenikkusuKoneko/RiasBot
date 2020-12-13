using System;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Profile
{
    [Name("Profile")]
    [Group("profile")]
    public class ProfileModule : RiasModule<ProfileService>
    {
        private readonly HttpClient _httpClient;
        
        public ProfileModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }

        [Command]
        [Context(ContextType.Guild)]
        [Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task ProfileAsync([Remainder] DiscordMember? member = null)
        {
            member ??= (DiscordMember) Context.User;
            await Context.Channel.TriggerTypingAsync();

            var serverAttachFilesPerm = Context.Guild!.CurrentMember.GetPermissions().HasPermission(Permissions.AttachFiles);
            var channelAttachFilesPerm = Context.Guild!.CurrentMember.PermissionsIn(Context.Channel).HasPermission(Permissions.AttachFiles);
            if (!serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.ProfileNoAttachFilesPermission);
                return;
            }

            if (serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.ProfileNoAttachFilesChannelPermission);
                return;
            }

            await using var profileImage = await Service.GenerateProfileImageAsync(member);
            await Context.Channel.SendFileAsync($"{member.Id}_profile.png", profileImage);
        }

        [Command("background", "bg", "cover", "image")]
        [Context(ContextType.Guild)]
        [Cooldown(1, 30, CooldownMeasure.Seconds, BucketType.User)]
        public async Task BackgroundAsync(string url)
        {
            var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UserEntity { UserId = Context.User.Id });
            if (userDb.Currency < 1000)
            {
                await ReplyErrorAsync(Localization.GamblingCurrencyNotEnough, Configuration.Currency);
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

            await Context.Channel.TriggerTypingAsync();
            
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
            
            var serverAttachFilesPerm = Context.Guild!.CurrentMember.GetPermissions().HasPermission(Permissions.AttachFiles);
            var channelAttachFilesPerm = Context.Guild!.CurrentMember.PermissionsIn(Context.Channel).HasPermission(Permissions.AttachFiles);
            if (!serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.ProfileBackgroundNoAttachFilesPermission);
                return;
            }

            if (serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.ProfileBackgroundNoAttachFilesChannelPermission);
                return;
            }

            backgroundStream.Position = 0;
            await using var profilePreview = await Service.GenerateProfileBackgroundAsync(Context.User, backgroundStream);
            await Context.Channel.SendFileAsync($"{Context.User.Id}_profile_preview.png", profilePreview, GetText(Localization.ProfileBackgroundPreview));
            
            var messageReceived = await NextMessageAsync();
            if (!string.Equals(messageReceived.Result?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync(Localization.ProfileBackgroundCanceled);
                return;
            }

            userDb.Currency -= 1000;
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity { UserId = Context.User.Id, BackgroundDim = 50 });
            profileDb.BackgroundUrl = url;
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileBackgroundSet);
        }

        [Command("dim")]
        [Context(ContextType.Guild)]
        public async Task DimAsync(int dim)
        {
            if (dim < 0 || dim > 100)
            {
                await ReplyErrorAsync(Localization.ProfileBackgroundDimBetween, 0, 100);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity { UserId = Context.User.Id });
            profileDb.BackgroundDim = dim;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileBackgroundDimSet, dim);
        }

        [Command("biography", "bio")]
        [Context(ContextType.Guild)]
        public async Task BiographyAsync([Remainder] string bio)
        {
            if (bio.Length > 200)
            {
                await ReplyErrorAsync(Localization.ProfileBiographyLimit, 200);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity { UserId = Context.User.Id, BackgroundDim = 50 });
            profileDb.Biography = bio;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileBiographySet);
        }

        [Command("colour", "color")]
        [Context(ContextType.Guild)]
        public async Task ColorAsync([Remainder] DiscordColor color)
        {
            if (!await Service.CheckColorAsync(Context.User, color))
            {
                await ReplyErrorAsync(Localization.ProfileInvalidColor, PatreonService.ProfileColorTier, Configuration.Patreon);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity { UserId = Context.User.Id, BackgroundDim = 50 });
            profileDb.Color = color.ToString();

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.ProfileColorSet);
        }

        [Command("badge")]
        [Context(ContextType.Guild)]
        public async Task BadgeAsync(int index, [Remainder] string? text = null)
        {
            index--;
            if (index < 0)
                index = 0;

            if (Configuration.PatreonConfiguration != null && Context.User.Id != Configuration.MasterId)
            {
                var patreonTier = (await DbContext.Patreon.FirstOrDefaultAsync(x => x.UserId == Context.User.Id))?.Tier ?? 0;
                switch (index)
                {
                    case 0 when patreonTier < PatreonService.ProfileFirstBadgeTier:
                        await ReplyErrorAsync(Localization.ProfileFirstBadgeNoPatreon, PatreonService.ProfileFirstBadgeTier, Configuration.Patreon);
                        return;
                    case 1 when patreonTier < PatreonService.ProfileSecondBadgeTier:
                        await ReplyErrorAsync(Localization.ProfileSecondBadgeNoPatreon, PatreonService.ProfileSecondBadgeTier, Configuration.Patreon);
                        return;
                    case 2 when patreonTier < PatreonService.ProfileThirdBadgeTier:
                        await ReplyErrorAsync(Localization.ProfileThirdBadgeNoPatreon, PatreonService.ProfileThirdBadgeTier, Configuration.Patreon);
                        return;
                }
            }

            if (index > 2)
            {
                await ReplyErrorAsync(Localization.ProfileBadgeError);
                return;
            }

            if (string.Equals(text, "master", StringComparison.InvariantCultureIgnoreCase) && Context.User.Id != Configuration.MasterId)
            {
                await ReplyErrorAsync(Localization.ProfileBadgeNotAvailable);
                return;
            }

            if (!string.IsNullOrEmpty(text) && text.Length > 20)
            {
                await ReplyErrorAsync(Localization.ProfileBadgeTextLimit);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new ProfileEntity { UserId = Context.User.Id, BackgroundDim = 50 });
            profileDb.Badges ??= new string[3];
                
            profileDb.Badges[index] = text!;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(string.IsNullOrEmpty(text) ? Localization.ProfileBadgeRemoved : Localization.ProfileBadgeSet);
        }
    }
}