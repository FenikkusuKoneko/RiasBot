using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Administration
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
             UserPermission(Permissions.ManageNicknames), BotPermission(Permissions.ManageNicknames),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetNicknameAsync(DiscordMember user, [Remainder] string? nickname = null)
            {
                if (user.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationNicknameOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await user.ModifyAsync(x => x.Nickname = nickname);

                if (string.IsNullOrEmpty(nickname))
                    await ReplyConfirmationAsync(Localization.AdministrationNicknameRemoved, user.FullName());
                else
                    await ReplyConfirmationAsync(Localization.AdministrationNicknameChanged, user.FullName(), nickname);
            }
            
            [Command("setmynickname"), Context(ContextType.Guild),
             BotPermission(Permissions.ManageNicknames),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetMyNicknameAsync([Remainder] string? nickname = null)
            {
                var member = (DiscordMember) Context.User;
                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationNicknameYouOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationYouAboveMe);
                    return;
                }

                if (!member.GetPermissions().HasPermission(Permissions.ChangeNickname))
                {
                    await ReplyErrorAsync(Localization.AdministrationChangeNicknamePermission);
                    return;
                }
                
                await member.ModifyAsync(x => x.Nickname = nickname);

                if (string.IsNullOrEmpty(nickname))
                    await ReplyConfirmationAsync(Localization.AdministrationYourNicknameRemoved, member.FullName());
                else
                    await ReplyConfirmationAsync(Localization.AdministrationYourNicknameChanged, member.FullName(), nickname);
            }
            
            [Command("setservername"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageGuild), BotPermission(Permissions.ManageGuild),
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
             UserPermission(Permissions.ManageGuild), BotPermission(Permissions.ManageGuild),
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