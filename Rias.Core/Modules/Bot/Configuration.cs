using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Implementation;
using Rias.Interactive;

namespace Rias.Core.Modules.Bot
{
    public partial class Bot
    {
        [Name("Configuration")]
        public class Configuration : RiasModule
        {
            private readonly InteractiveService _interactive;
            private readonly HttpClient _httpClient;

            public Configuration(IServiceProvider services) : base(services)
            {
                _interactive = services.GetRequiredService<InteractiveService>();
                _httpClient = services.GetRequiredService<HttpClient>();
            }

            [Command("setusername"),
             OwnerOnly]
            public async Task SetUsernameAsync([Remainder] string username)
            {
                if (username.Length < 2 || username.Length > 32)
                {
                    await ReplyErrorAsync("UsernameLengthLimit");
                    return;
                }

                await ReplyConfirmationAsync("SetUsernameDialog");
                var message = await _interactive.NextMessageAsync(Context.Message);
                if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync("SetUsernameCanceled");
                    return;
                }
                
                try
                {
                    await Context.Client.CurrentUser.ModifyAsync(u => u.Username = username);
                    await ReplyConfirmationAsync("UsernameSet", username);
                }
                catch
                {
                    await ReplyErrorAsync("SetUsernameError");
                }
            }

            [Command("setavatar"),
             OwnerOnly]
            public async Task SetAvatarAsync(string url)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    await ReplyErrorAsync("#Utility_UrlNotValid");
                    return;
                }
                
                using var response = await _httpClient.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    await ReplyErrorAsync("#Utility_UrlNotValid");
                    return;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                if (!(RiasUtils.IsPng(stream) || RiasUtils.IsJpg(stream)))
                {
                    await ReplyErrorAsync("#Utility_UrlNotPngJpg");
                    return;
                }

                stream.Position = 0;
                using var image = new Image(stream);

                try
                {
                    await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = image);
                    await ReplyConfirmationAsync("AvatarSet");
                }
                catch
                {
                    await ReplyErrorAsync("SetAvatarError");
                }
            }
        }
    }
}