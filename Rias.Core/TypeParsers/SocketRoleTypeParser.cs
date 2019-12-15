using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class SocketRoleTypeParser : RiasTypeParser<SocketRole>
    {
        public override ValueTask<TypeParserResult<SocketRole>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            if (context.Guild is null)
                return TypeParserResult<SocketRole>.Unsuccessful("#TypeParser_SocketRoleNotGuild");

            SocketRole role;
            if (MentionUtils.TryParseRole(value, out var roleId))
            {
                role = context.Guild.GetRole(roleId);
                if (role != null)
                    return TypeParserResult<SocketRole>.Successful(role);
            }

            if (ulong.TryParse(value, out var id))
            {
                role = context.Guild.GetRole(id);
                if (role != null)
                    return TypeParserResult<SocketRole>.Successful(role);
            }

            role = context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));
            if (role != null)
                return TypeParserResult<SocketRole>.Successful(role);

            if (parameter.IsOptional && parameter.Attributes.Any(x => x is SuppressWarningAttribute))
                return TypeParserResult<SocketRole>.Successful((SocketRole)parameter.DefaultValue);

            return TypeParserResult<SocketRole>.Unsuccessful("#Administration_RoleNotFound");
        }
    }
}