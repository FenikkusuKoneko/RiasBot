using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class CachedRoleTypeParser : RiasTypeParser<CachedRole>
    {
        public override ValueTask<TypeParserResult<CachedRole>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<CachedRole>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedRoleNotGuild));

            CachedRole role;
            if (Discord.TryParseRoleMention(value, out var roleId))
            {
                role = context.Guild.GetRole(roleId);
                if (role != null)
                    return TypeParserResult<CachedRole>.Successful(role);
            }

            if (ulong.TryParse(value, out var id))
            {
                role = context.Guild.GetRole(id);
                if (role != null)
                    return TypeParserResult<CachedRole>.Successful(role);
            }

            role = context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (role != null)
                return TypeParserResult<CachedRole>.Successful(role);

            if (parameter.IsOptional)
                return TypeParserResult<CachedRole>.Successful((CachedRole)parameter.DefaultValue);

            return TypeParserResult<CachedRole>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationRoleNotFound));
        }
    }
}