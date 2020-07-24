using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class CachedVoiceChannelTypeParser : RiasTypeParser<CachedVoiceChannel>
    {
        public override ValueTask<TypeParserResult<CachedVoiceChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<CachedVoiceChannel>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedVoiceChannelNotGuild));

            CachedVoiceChannel channel;

            if (Snowflake.TryParse(value, out var id))
            {
                channel = context.Guild.GetVoiceChannel(id);
                if (channel != null)
                    return TypeParserResult<CachedVoiceChannel>.Successful(channel);
            }

            channel = context.Guild.VoiceChannels.FirstOrDefault(c => string.Equals(c.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (channel != null)
                return TypeParserResult<CachedVoiceChannel>.Successful(channel);

            return TypeParserResult<CachedVoiceChannel>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationVoiceChannelNotFound));
        }
    }
}