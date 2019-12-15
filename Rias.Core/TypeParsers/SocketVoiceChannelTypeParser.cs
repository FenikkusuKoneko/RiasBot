using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class SocketVoiceChannelTypeParser : RiasTypeParser<SocketVoiceChannel>
    {
        public override ValueTask<TypeParserResult<SocketVoiceChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            if (context.Guild is null)
                return TypeParserResult<SocketVoiceChannel>.Unsuccessful("#TypeParser_SocketVoiceChannelNotGuild");

            SocketVoiceChannel channel;

            if (MentionUtils.TryParseChannel(value, out var channelId))
            {
                channel = context.Guild.GetVoiceChannel(channelId);
                if (channel != null)
                    return TypeParserResult<SocketVoiceChannel>.Successful(channel);
            }

            if (ulong.TryParse(value, out var id))
            {
                channel = context.Guild.GetVoiceChannel(id);
                if (channel != null)
                    return TypeParserResult<SocketVoiceChannel>.Successful(channel);
            }

            channel = context.Guild.VoiceChannels.FirstOrDefault(c => string.Equals(c.Name, value, StringComparison.OrdinalIgnoreCase));
            if (channel != null)
                return TypeParserResult<SocketVoiceChannel>.Successful(channel);

            if (parameter.IsOptional)
                return TypeParserResult<SocketVoiceChannel>.Successful((SocketVoiceChannel) parameter.DefaultValue);

            return TypeParserResult<SocketVoiceChannel>.Unsuccessful("#Administration_VoiceChannelNotFound");
        }
    }
}