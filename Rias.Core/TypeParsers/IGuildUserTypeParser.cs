using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class IGuildUserTypeParser : RiasTypeParser<IGuildUser>
    {
        public override ValueTask<TypeParserResult<IGuildUser>> ParseAsync(Parameter parameter, string value, RiasCommandContext context, IServiceProvider provider)
        {
            if (context.Guild is null)
                return TypeParserResult<IGuildUser>.Unsuccessful("The IGuildUser TypeParser cannot be used in a context without a guild!");

            SocketGuildUser user;

            if (MentionUtils.TryParseUser(value, out var userId))
            {
                user = context.Guild.GetUser(userId);
                if (user != null)
                    return TypeParserResult<IGuildUser>.Successful(user);
            }
            
            if (uint.TryParse(value, out var id))
            {
                user = context.Guild.GetUser(id);
                if (user != null)
                    return TypeParserResult<IGuildUser>.Successful(user);
            }

            var users = context.Guild.Users;

            var index = value.LastIndexOf("#", StringComparison.Ordinal);
            if (index >= 0)
            {
                var username = value.Substring(0, index);
                if (ushort.TryParse(value.Substring(index + 1), out var discriminator))
                {
                    user = users.FirstOrDefault(u => u.DiscriminatorValue == discriminator
                                                                       && string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                    if (user != null)
                        return TypeParserResult<IGuildUser>.Successful(user);
                }
            }
            
            user = users.FirstOrDefault(u => string.Equals(u.Username, value, StringComparison.OrdinalIgnoreCase));
            if (user != null)
                return TypeParserResult<IGuildUser>.Successful(user);
            
            user = users.FirstOrDefault(u => string.Equals(u.Nickname, value, StringComparison.OrdinalIgnoreCase));
            if (user != null)
                return TypeParserResult<IGuildUser>.Successful(user);
            
            return TypeParserResult<IGuildUser>.Unsuccessful("The user couldn't be found in the guild!");
        }
    }
}