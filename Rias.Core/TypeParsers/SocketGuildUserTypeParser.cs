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
    public class SocketGuildUserTypeParser : RiasTypeParser<SocketGuildUser>
    {
        public override ValueTask<TypeParserResult<SocketGuildUser>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            if (context.Guild is null)
                return TypeParserResult<SocketGuildUser>.Unsuccessful("#TypeParser_SocketGuildUserNotGuild");

            SocketGuildUser user;

            if (MentionUtils.TryParseUser(value, out var userId))
            {
                user = context.Guild.GetUser(userId);
                if (user != null)
                    return TypeParserResult<SocketGuildUser>.Successful(user);
            }

            if (ulong.TryParse(value, out var id))
            {
                user = context.Guild.GetUser(id);
                if (user != null)
                    return TypeParserResult<SocketGuildUser>.Successful(user);
            }

            var users = context.Guild.Users;

            var index = value.LastIndexOf("#", StringComparison.Ordinal);
            if (index > 0)
            {
                var username = value[..index];
                if (ushort.TryParse(value[(index+1)..], out var discriminator))
                {
                    user = users.FirstOrDefault(u => u.DiscriminatorValue == discriminator && string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                    if (user != null)
                        return TypeParserResult<SocketGuildUser>.Successful(user);
                }
            }

            user = users.FirstOrDefault(u => string.Equals(u.Username, value, StringComparison.OrdinalIgnoreCase));
            if (user != null)
                return TypeParserResult<SocketGuildUser>.Successful(user);

            user = users.FirstOrDefault(u => string.Equals(u.Nickname, value, StringComparison.OrdinalIgnoreCase));
            if (user != null)
                return TypeParserResult<SocketGuildUser>.Successful(user);

            if (parameter.IsOptional && parameter.Attributes.Any(x => x is SuppressWarningAttribute))
                return TypeParserResult<SocketGuildUser>.Successful((SocketGuildUser) parameter.DefaultValue);

            return TypeParserResult<SocketGuildUser>.Unsuccessful("#Administration_UserNotFound");
        }
    }
}