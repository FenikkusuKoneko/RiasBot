using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Server")]
        public class ServerSubmodule : RiasModule
        {
            private readonly HttpClient _httpClient;
            
            public ServerSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }
            
            [Command("setnickname"), Context(ContextType.Guild),
             UserPermission(Permission.ManageNicknames), BotPermission(Permission.ManageNicknames),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetNicknameAsync(CachedMember user, [Remainder] string? nickname = null)
            {
                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationNicknameOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await user.ModifyAsync(x => x.Nick = nickname);

                if (string.IsNullOrEmpty(nickname))
                    await ReplyConfirmationAsync(Localization.AdministrationNicknameRemoved, user);
                else
                    await ReplyConfirmationAsync(Localization.AdministrationNicknameChanged, user, nickname);
            }
            
            [Command("setmynickname"), Context(ContextType.Guild),
             BotPermission(Permission.ManageNicknames),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetMyNicknameAsync([Remainder] string? nickname = null)
            {
                var user = (CachedMember) Context.User;
                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationNicknameYouOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationYouAboveMe);
                    return;
                }

                if (!user.Permissions.ChangeNickname)
                {
                    await ReplyErrorAsync(Localization.AdministrationChangeNicknamePermission);
                    return;
                }
                
                await user.ModifyAsync(x => x.Nick = nickname);

                if (string.IsNullOrEmpty(nickname))
                    await ReplyConfirmationAsync(Localization.AdministrationYourNicknameRemoved, user);
                else
                    await ReplyConfirmationAsync(Localization.AdministrationYourNicknameChanged, user, nickname);
            }
            
            [Command("setservername"), Context(ContextType.Guild),
             UserPermission(Permission.ManageGuild), BotPermission(Permission.ManageGuild),
             Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetServerNameAsync([Remainder] string name)
            {
                if (name.Length < 2 || name.Length > 100)
                {
                    await ReplyErrorAsync(Localization.AdministrationServerNameLengthLimit);
                    return;
                }

                await Context.Guild!.ModifyAsync(x => x.Name = name);
                await ReplyConfirmationAsync(Localization.AdministrationServerNameChanged, name);
            }

            [Command("setservericon"), Context(ContextType.Guild),
             UserPermission(Permission.ManageGuild), BotPermission(Permission.ManageGuild),
             Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetServerIconAsync(string url)
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await ReplyErrorAsync(Localization.UtilityImageOrUrlNotGood);
                    return;
                }

                try
                {
                    await using var stream = await _httpClient.GetStreamAsync(url);
                    await using var iconStream = new MemoryStream();
                    await stream.CopyToAsync(iconStream);
                    iconStream.Position = 0;
                    
                    await Context.Guild!.ModifyAsync(x => x.Icon = iconStream);
                    await ReplyConfirmationAsync(Localization.AdministrationServerIconChanged);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.UtilityImageOrUrlNotGood);
                }
            }
        }
    }
}