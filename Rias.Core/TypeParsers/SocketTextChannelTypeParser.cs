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
    public class SocketTextChannelTypeParser : RiasTypeParser<SocketTextChannel>
    {
        public override ValueTask<TypeParserResult<SocketTextChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            if (context.Guild is null)
                return TypeParserResult<SocketTextChannel>.Unsuccessful("#TypeParser_SocketTextChannelNotGuild");

            SocketTextChannel channel;

            if (MentionUtils.TryParseChannel(value, out var channelId))
            {
                channel = context.Guild.GetTextChannel(channelId);
                if (channel != null)
                    return TypeParserResult<SocketTextChannel>.Successful(channel);
            }

            if (ulong.TryParse(value, out var id))
            {
                channel = context.Guild.GetTextChannel(id);
                if (channel != null)
                    return TypeParserResult<SocketTextChannel>.Successful(channel);
            }

            channel = context.Guild.TextChannels.FirstOrDefault(c => string.Equals(c.Name, value, StringComparison.OrdinalIgnoreCase));
            if (channel != null)
                return TypeParserResult<SocketTextChannel>.Successful(channel);

            if (parameter.IsOptional && parameter.Attributes.Any(x => x is SuppressWarningAttribute))
                return TypeParserResult<SocketTextChannel>.Successful((SocketTextChannel) parameter.DefaultValue);

            return TypeParserResult<SocketTextChannel>.Unsuccessful("#Administration_TextChannelNotFound");
        }
    }
}