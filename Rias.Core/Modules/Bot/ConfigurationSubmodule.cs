using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Bot
{
    public partial class BotModule
    {
        [Name("Configuration")]
        public class ConfigurationSubmodule : RiasModule
        {
            private readonly HttpClient _httpClient;
            
            public ConfigurationSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }
            
            [Command("setusername"), OwnerOnly]
            public async Task SetUsernameAsync([Remainder] string username)
            {
                if (username.Length < 2 || username.Length > 32)
                {
                    await ReplyErrorAsync(Localization.BotUsernameLengthLimit);
                    return;
                }

                await ReplyConfirmationAsync(Localization.BotSetUsernameDialog);
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived?.Message.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.BotSetUsernameCanceled);
                    return;
                }
                
                try
                {
                    await RiasBot.CurrentUser.ModifyAsync(u => u.Name = username);
                    await ReplyConfirmationAsync(Localization.BotUsernameSet, username);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.BotSetUsernameError);
                }
            }

            [Command("setavatar"), OwnerOnly]
            public async Task SetAvatarAsync(string url)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotValid);
                    return;
                }
                
                using var response = await _httpClient.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotValid);
                    return;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var avatarStream = new MemoryStream();
                await stream.CopyToAsync(avatarStream);
                avatarStream.Position = 0;
                
                if (!(RiasUtilities.IsPng(stream) || RiasUtilities.IsJpg(stream)))
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotPngJpg);
                    return;
                }

                avatarStream.Position = 0;

                try
                {
                    await RiasBot.CurrentUser.ModifyAsync(x => x.Avatar = avatarStream);
                    await ReplyConfirmationAsync(Localization.BotAvatarSet);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.BotSetAvatarError);
                }
            }
        }
    }
}