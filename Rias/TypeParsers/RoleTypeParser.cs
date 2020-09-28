using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Implementation;

namespace Rias.TypeParsers
{
    public class RoleTypeParser : RiasTypeParser<DiscordRole>
    {
        public override ValueTask<TypeParserResult<DiscordRole>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<DiscordRole>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedRoleNotGuild));

            DiscordRole? role;
            if (!RiasUtilities.TryParseRoleMention(value, out var roleId) || ulong.TryParse(value, out roleId))
            {
                role = context.Guild.GetRole(roleId);
                if (role != null)
                    return TypeParserResult<DiscordRole>.Successful(role);
                
                return TypeParserResult<DiscordRole>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationRoleNotFound));
            }

            role = context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (role != null)
                return TypeParserResult<DiscordRole>.Successful(role);

            return TypeParserResult<DiscordRole>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationRoleNotFound));
        }
    }
}