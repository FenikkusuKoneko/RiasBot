using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Implementation;
using Serilog;

namespace Rias.TypeParsers
{
    public class MemberTypeParser : RiasTypeParser<DiscordMember>
    {
        public override async ValueTask<TypeParserResult<DiscordMember>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<DiscordMember>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedMemberNotGuild));

            var riasBot = context.ServiceProvider.GetRequiredService<RiasBot>();
            if (!riasBot.DownloadedMembers.Contains(context.Guild.Id))
            {
                riasBot.DownloadedMembers.Add(context.Guild.Id);
                await context.Guild.RequestMembersAsync();
                Log.Debug($"Members requested for {context.Guild.Name} ({context.Guild.Id})");
            }

            DiscordMember member;
            if (RiasUtilities.TryParseUserMention(value, out var memberId) || ulong.TryParse(value, out memberId))
            {
                try
                {
                    member = await context.Guild.GetMemberAsync(memberId);
                    return TypeParserResult<DiscordMember>.Successful(member);
                }
                catch
                {
                    return TypeParserResult<DiscordMember>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationUserNotFound));
                }
            }

            var members = context.Guild.Members;

            var index = value.LastIndexOf("#", StringComparison.Ordinal);
            if (index > 0)
            {
                var username = value[..index];
                var discriminator = value[(index + 1)..];
                if (discriminator.Length == 4 && int.TryParse(discriminator, out _))
                {
                    member = members.FirstOrDefault(u => string.Equals(u.Value.Discriminator, discriminator)
                                                         && string.Equals(u.Value.Username, username, StringComparison.OrdinalIgnoreCase)).Value;
                    if (member != null)
                        return TypeParserResult<DiscordMember>.Successful(member);
                }
            }

            member = members.FirstOrDefault(u => string.Equals(u.Value.Nickname, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (member != null)
                return TypeParserResult<DiscordMember>.Successful(member);

            member = members.FirstOrDefault(u => string.Equals(u.Value.Username, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (member != null)
                return TypeParserResult<DiscordMember>.Successful(member);
            
            return TypeParserResult<DiscordMember>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationUserNotFound));
        }
    }
}