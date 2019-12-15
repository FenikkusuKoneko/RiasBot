using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Server")]
        public class Server : RiasModule
        {
            private readonly HttpClient _httpClient;

            public Server(IServiceProvider services) : base(services)
            {
                _httpClient = services.GetRequiredService<HttpClient>();
            }

            [Command("setnickname"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageNicknames), BotPermission(GuildPermission.ManageNicknames),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetNicknameAsync(SocketGuildUser user, [Remainder] string? nickname = null)
            {
                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("NicknameOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                await user.ModifyAsync(x => x.Nickname = nickname);

                if (string.IsNullOrEmpty(nickname))
                    await ReplyConfirmationAsync("NicknameRemoved", user);
                else
                    await ReplyConfirmationAsync("NicknameChanged", user, nickname);
            }

            [Command("setservername"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageGuild), BotPermission(GuildPermission.ManageGuild),
             Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetServerNameAsync([Remainder] string name)
            {
                if (name.Length < 2 || name.Length > 100)
                {
                    await ReplyErrorAsync("ServerNameLengthLimit");
                    return;
                }

                await Context.Guild!.ModifyAsync(x => x.Name = name);
                await ReplyConfirmationAsync("ServerNameChanged", name);
            }

            [Command("setservericon"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageGuild), BotPermission(GuildPermission.ManageGuild),
             Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetServerIconAsync(string url)
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await ReplyErrorAsync("#Utility_ImageOrUrlNotGood");
                    return;
                }

                try
                {
                    await using var stream = await _httpClient.GetStreamAsync(url);
                    using var image = new Image(stream);
                    await Context.Guild!.ModifyAsync(x => x.Icon = image);

                    await ReplyConfirmationAsync("ServerIconChanged");
                }
                catch
                {
                    await ReplyErrorAsync("#Utility_ImageOrUrlNotGood");
                }
            }
        }
    }
}