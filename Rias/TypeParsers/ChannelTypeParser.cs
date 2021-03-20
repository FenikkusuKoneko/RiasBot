using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.TypeParsers
{
#pragma warning disable 8509
    public class ChannelTypeParser : RiasTypeParser<DiscordChannel>
    {
        public override ValueTask<TypeParserResult<DiscordChannel>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var channelType = ChannelType.Unknown;
            foreach (var attribute in parameter.Attributes)
            {
                channelType = attribute switch
                {
                    CategoryChannelAttribute => ChannelType.Category,
                    TextChannelAttribute => ChannelType.Text,
                    VoiceChannelAttribute => ChannelType.Voice,
                    _ => channelType
                };

                if (channelType != ChannelType.Unknown)
                    break;
            }

            var localization = context.Services.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<DiscordChannel>.Failed(localization.GetText(context.Guild?.Id, Localization.TypeParserChannelNotGuild));

            DiscordChannel? channel;
            if (RiasUtilities.TryParseChannelMention(value, out var channelId) || ulong.TryParse(value, out channelId))
            {
                channel = context.Guild.GetChannel(channelId);

                var channelTypeBool = channelType == ChannelType.Unknown || channelType == ChannelType.Text
                    ? channel.Type == ChannelType.Text || channel.Type == ChannelType.News || channel.Type == ChannelType.Store
                    : channel.Type == channelType;
                
                if (channel != null && channelTypeBool)
                    return TypeParserResult<DiscordChannel>.Successful(channel);
            }
            else
            {
                channel = channelType switch
                {
                    ChannelType.Category => context.Guild.GetCategoryChannel(value),
                    ChannelType.Text => context.Guild.GetTextChannel(value),
                    ChannelType.Voice => context.Guild.GetVoiceChannel(value),
                    ChannelType.Unknown => context.Guild.Channels
                        .OrderBy(c => c.Value.Position)
                        .FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value
                };
            
                if (channel != null)
                    return TypeParserResult<DiscordChannel>.Successful(channel);
            }

            return channelType switch
            {
                ChannelType.Category => TypeParserResult<DiscordChannel>.Failed(localization.GetText(context.Guild.Id, Localization.AdministrationCategoryChannelNotFound)),
                ChannelType.Text => TypeParserResult<DiscordChannel>.Failed(localization.GetText(context.Guild.Id, Localization.AdministrationTextChannelNotFound)),
                ChannelType.Voice => TypeParserResult<DiscordChannel>.Failed(localization.GetText(context.Guild.Id, Localization.AdministrationVoiceChannelNotFound)),
                ChannelType.Unknown => TypeParserResult<DiscordChannel>.Failed(localization.GetText(context.Guild.Id, Localization.AdministrationChannelNotFound))
            };
        }
    }
#pragma warning restore 8509
}