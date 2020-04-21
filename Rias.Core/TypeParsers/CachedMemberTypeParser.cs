using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class CachedMemberTypeParser : RiasTypeParser<CachedMember>
    {
        public override ValueTask<TypeParserResult<CachedMember>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<CachedMember>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedMemberNotGuild));

            CachedMember member;

            if (Discord.TryParseUserMention(value, out var userId))
            {
                member = context.Guild.GetMember(userId);
                if (member != null)
                    return TypeParserResult<CachedMember>.Successful(member);
            }

            if (Snowflake.TryParse(value, out var id))
            {
                member = context.Guild.GetMember(id);
                if (member != null)
                    return TypeParserResult<CachedMember>.Successful(member);
            }

            var members = context.Guild.Members;

            var index = value.LastIndexOf("#", StringComparison.Ordinal);
            if (index > 0)
            {
                var username = value[..index];
                var discriminator = value[(index+1)..];
                if (discriminator.Length == 4 && int.TryParse(discriminator, out _))
                {
                    member = members.FirstOrDefault(u => string.Equals(u.Value.Discriminator, discriminator)
                                                         && string.Equals(u.Value.Name, username, StringComparison.OrdinalIgnoreCase)).Value;
                    if (member != null)
                        return TypeParserResult<CachedMember>.Successful(member);
                }
            }

            member = members.FirstOrDefault(u => string.Equals(u.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (member != null)
                return TypeParserResult<CachedMember>.Successful(member);

            member = members.FirstOrDefault(u => string.Equals(u.Value.Nick, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (member != null)
                return TypeParserResult<CachedMember>.Successful(member);

            if (parameter.IsOptional)
                return TypeParserResult<CachedMember>.Successful((CachedMember) parameter.DefaultValue);

            return TypeParserResult<CachedMember>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationUserNotFound));
        }
    }
}