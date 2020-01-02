using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Models;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;

namespace Rias.Core.Modules.Profile
{
    [Name("Profile")]
    public class Profile : RiasModule<ProfileService>
    {
        private readonly HttpClient _httpClient;
        private readonly InteractiveService _interactive;
        
        public Profile(IServiceProvider services) : base(services)
        {
            _httpClient = services.GetRequiredService<HttpClient>();
            _interactive = services.GetRequiredService<InteractiveService>();
        }

        [Command, Context(ContextType.Guild),
         Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task ProfileAsync(SocketGuildUser? user = null)
        {
            user ??= (SocketGuildUser) Context.User;
            using var unused = Context.Channel.EnterTypingState();

            await using var profileImage = await Service.GenerateProfileImageAsync(user);
            await Context.Channel.SendFileAsync(profileImage, $"{user.Id}_profile.png");
        }

        [Command("background"), Context(ContextType.Guild),
         Cooldown(1, 30, CooldownMeasure.Seconds, BucketType.User)]
        public async Task BackgroundAsync(string url)
        {
            var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Users {UserId = Context.User.Id});
            if (userDb.Currency < 1000)
            {
                await ReplyErrorAsync("#Gambling_CurrencyNotEnough", Credentials.Currency);
                return;
            }
            
            if (!Uri.TryCreate(url, UriKind.Absolute, out var backgroundUri))
            {
                await ReplyErrorAsync("#Utility_UrlNotValid");
                Context.Command.ResetCooldowns();
                return;
            }
            
            if (backgroundUri.Scheme != Uri.UriSchemeHttps)
            {
                await ReplyErrorAsync("#Utility_UrlNotHttps");
                Context.Command.ResetCooldowns();
                return;
            }
            
            using var unused = Context.Channel.EnterTypingState();
            
            using var result = await _httpClient.GetAsync(backgroundUri);
            if (!result.IsSuccessStatusCode)
            {
                await ReplyErrorAsync("#Utility_ImageOrUrlNotGood");
                Context.Command.ResetCooldowns();
                return;
            }

            await using var backgroundStream = await result.Content.ReadAsStreamAsync();
            if (!(RiasUtils.IsPng(backgroundStream) || RiasUtils.IsJpg(backgroundStream)))
            {
                await ReplyErrorAsync("#Utility_UrlNotPngJpg");
                return;
            }

            backgroundStream.Position = 0;
            await using var profilePreview = await Service.GenerateProfileBackgroundAsync(Context.User, backgroundStream);
            await Context.Channel.SendFileAsync(profilePreview, $"{Context.User.Id}_profile_preview.png", GetText("BackgroundPreview"));
            
            var message = await _interactive.NextMessageAsync(Context.Message);
            if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync("BackgroundCanceled");
                return;
            }

            userDb.Currency -= 1000;
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Database.Models.Profile {UserId = Context.User.Id, BackgroundDim = 50});
            profileDb.BackgroundUrl = url;
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("BackgroundSet");
        }

        [Command("dim"), Context(ContextType.Guild)]
        public async Task DimAsync(int dim)
        {
            if (dim < 0 || dim > 100)
            {
                await ReplyErrorAsync("BackgroundDimBetween", 0, 100);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Database.Models.Profile {UserId = Context.User.Id});
            profileDb.BackgroundDim = dim;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("BackgroundDimSet", dim);
        }
        
        [Command("biography"), Context(ContextType.Guild)]
        public async Task BiographyAsync([Remainder] string bio)
        {
            if (bio.Length > 200)
            {
                await ReplyErrorAsync("BiographyLimit", 200);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Database.Models.Profile {UserId = Context.User.Id, BackgroundDim = 50});
            profileDb.Biography = bio;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("BiographySet");
        }
        
        [Command("color"), Context(ContextType.Guild)]
        public async Task ColorAsync([Remainder] Color color)
        {
            if (!await Service.CheckColorAsync(Context.User, color))
            {
                await ReplyErrorAsync("InvalidColor", Credentials.Patreon);
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Database.Models.Profile {UserId = Context.User.Id, BackgroundDim = 50});
            profileDb.Color = color.ToString();

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("ColorSet");
        }

        [Command("badge"), Context(ContextType.Guild)]
        public async Task BadgeAsync(int index, string text)
        {
            index--;
            if (index < 0)
                index = 0;

            if (Credentials.PatreonConfig != null && Context.User.Id != Credentials.MasterId)
            {
                var patreonTier = (await DbContext.Patreon.FirstOrDefaultAsync(x => x.UserId == Context.User.Id))?.Tier ?? 0;
                switch (index)
                {
                    case 0 when patreonTier < 3:
                        await ReplyErrorAsync("FirstBadgeNoPatreon", Credentials.Patreon);
                        return;
                    case 1 when patreonTier < 5:
                        await ReplyErrorAsync("SecondBadgeNoPatreon", Credentials.Patreon);
                        return;
                    case 2 when patreonTier < 6:
                        await ReplyErrorAsync("ThirdBadgeNoPatreon", Credentials.Patreon);
                        return;
                }
            }

            if (index > 2)
            {
                await ReplyErrorAsync("BadgeError");
                return;
            }

            if (string.Equals(text, "master", StringComparison.InvariantCultureIgnoreCase) && Context.User.Id != Credentials.MasterId)
            {
                await ReplyErrorAsync("BadgeNotAvailable");
                return;
            }

            if (text.Length > 20)
            {
                await ReplyErrorAsync("BadgeTextLimit");
                return;
            }
            
            var profileDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id,
                () => new Database.Models.Profile {UserId = Context.User.Id, BackgroundDim = 50});
            if (profileDb.Badges is null)
                profileDb.Badges = new string[3];
                
            profileDb.Badges[index] = text;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("BadgeSet");
        }
    }
}