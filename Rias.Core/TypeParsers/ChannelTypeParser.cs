using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class ChannelTypeParser : RiasTypeParser<DiscordChannel>
    {
        public override ValueTask<TypeParserResult<DiscordChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            if (!(parameter.Attributes.FirstOrDefault(x => x is ChannelAttribute) is ChannelAttribute channelAttribute))
                return TypeParserResult<DiscordChannel>.Unsuccessful("The channel doesn't have an attribute type specified");
            
            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<DiscordChannel>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserCachedTextChannelNotGuild));

            DiscordChannel? channel;
            if (!RiasUtilities.TryParseChannelMention(value, out var channelId))
                ulong.TryParse(value, out channelId);

            if (channelId != 0)
            {
                channel = context.Guild.GetChannel(channelId);
                if (channel != null && channel.Type == channelAttribute.ChannelType)
                    return TypeParserResult<DiscordChannel>.Successful(channel);
            }

            channel = channelAttribute.ChannelType switch
            {
                ChannelType.Category => context.Guild.GetCategoryChannel(value),
                ChannelType.Text => context.Guild.GetTextChannel(value),
                ChannelType.Voice => context.Guild.GetVoiceChannel(value),
                _ => null
            };
            
            if (channel != null)
                return TypeParserResult<DiscordChannel>.Successful(channel);

#pragma warning disable 8509
            return channelAttribute.ChannelType switch
#pragma warning restore 8509
            {
                ChannelType.Category => TypeParserResult<DiscordChannel>.Unsuccessful(localization.GetText(context.Guild.Id, Localization.AdministrationCategoryChannelNotFound)),
                ChannelType.Text => TypeParserResult<DiscordChannel>.Unsuccessful(localization.GetText(context.Guild.Id, Localization.AdministrationTextChannelNotFound)),
                ChannelType.Voice => TypeParserResult<DiscordChannel>.Unsuccessful(localization.GetText(context.Guild.Id, Localization.AdministrationVoiceChannelNotFound))
            };
        }
    }
}