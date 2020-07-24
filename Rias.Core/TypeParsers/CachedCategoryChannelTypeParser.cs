using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class CachedCategoryChannelTypeParser : RiasTypeParser<CachedCategoryChannel>
    {
        public override ValueTask<TypeParserResult<CachedCategoryChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<CachedCategoryChannel>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedCategoryChannelNotGuild));

            CachedCategoryChannel channel;

            if (Discord.TryParseChannelMention(value, out var channelId))
            {
                channel = context.Guild.GetCategoryChannel(channelId);
                if (channel != null)
                    return TypeParserResult<CachedCategoryChannel>.Successful(channel);
            }

            if (Snowflake.TryParse(value, out var id))
            {
                channel = context.Guild.GetCategoryChannel(id);
                if (channel != null)
                    return TypeParserResult<CachedCategoryChannel>.Successful(channel);
            }

            channel = context.Guild.CategoryChannels.FirstOrDefault(c => string.Equals(c.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            if (channel != null)
                return TypeParserResult<CachedCategoryChannel>.Successful(channel);

            return TypeParserResult<CachedCategoryChannel>.Unsuccessful(localization.GetText(context.Guild.Id, Localization.AdministrationCategoryChannelNotFound));
        }
    }
}