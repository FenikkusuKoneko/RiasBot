using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class CachedTextChannelTypeParser : RiasTypeParser<CachedTextChannel>
    {
        public override ValueTask<TypeParserResult<CachedTextChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<CachedTextChannel>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedTextChannelNotGuild));

            CachedTextChannel channel;

            if (Discord.TryParseChannelMention(value, out var channelId))
            {
                channel = context.Guild.GetTextChannel(channelId);
                if (channel != null)
                    return TypeParserResult<CachedTextChannel>.Successful(channel);
            }

            if (ulong.TryParse(value, out var id))
            {
                channel = context.Guild.GetTextChannel(id);
                if (channel != null)
                    return TypeParserResult<CachedTextChannel>.Successful(channel);
            }

            channel = context.Guild.TextChannels.FirstOrDefault(c => string.Equals(c.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (channel != null)
                return TypeParserResult<CachedTextChannel>.Successful(channel);

            if (parameter.IsOptional)
                return TypeParserResult<CachedTextChannel>.Successful((CachedTextChannel) parameter.DefaultValue);

            return TypeParserResult<CachedTextChannel>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.AdministrationTextChannelNotFound));
        }
    }
}