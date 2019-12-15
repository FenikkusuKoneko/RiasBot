using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class SocketCategoryChannelTypeParser : RiasTypeParser<SocketCategoryChannel>
    {
        public override ValueTask<TypeParserResult<SocketCategoryChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            if (context.Guild is null)
                return TypeParserResult<SocketCategoryChannel>.Unsuccessful("#TypeParser_SocketCategoryChannelNotGuild");

            SocketCategoryChannel channel;

            if (MentionUtils.TryParseChannel(value, out var channelId))
            {
                channel = context.Guild.GetCategoryChannel(channelId);
                if (channel != null)
                    return TypeParserResult<SocketCategoryChannel>.Successful(channel);
            }

            if (ulong.TryParse(value, out var id))
            {
                channel = context.Guild.GetCategoryChannel(id);
                if (channel != null)
                    return TypeParserResult<SocketCategoryChannel>.Successful(channel);
            }

            channel = context.Guild.CategoryChannels.FirstOrDefault(c => string.Equals(c.Name, value, StringComparison.OrdinalIgnoreCase));
            if (channel != null)
                return TypeParserResult<SocketCategoryChannel>.Successful(channel);

            if (parameter.IsOptional)
                return TypeParserResult<SocketCategoryChannel>.Successful((SocketCategoryChannel) parameter.DefaultValue);

            return TypeParserResult<SocketCategoryChannel>.Unsuccessful("#Administration_CategoryChannelNotFound");
        }
    }
}