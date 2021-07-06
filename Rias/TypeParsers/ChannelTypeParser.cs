using System;
using System.Collections.Generic;
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
            var localization = context.Services.GetRequiredService<Localization>();
            if (context.Guild is null)
                return TypeParserResult<DiscordChannel>.Failed(localization.GetText(context.Guild?.Id, Localization.TypeParserChannelNotGuild));
            
            var channelType = ChannelType.Unknown;
            HashSet<ChannelType>? ignoreChannelTypes = null;
            
            foreach (var attribute in parameter.Attributes)
            {
                switch (attribute)
                {
                    case CategoryChannelAttribute:
                        channelType = ChannelType.Category;
                        break;
                    case TextChannelAttribute:
                        channelType = ChannelType.Text;
                        break;
                    case VoiceChannelAttribute:
                        channelType = ChannelType.Voice;
                        break;
                    case IgnoreChannelTypesAttribute ignoreChannelTypeAttribute:
                        ignoreChannelTypes = ignoreChannelTypeAttribute.ChannelTypes;
                        break;
                }
            }

            if (channelType is not ChannelType.Unknown && ignoreChannelTypes is not null && ignoreChannelTypes.Contains(channelType))
                throw new ArgumentException("The required channel type and the ignored channel type cannot be the same");

            DiscordChannel? channel;
            if (RiasUtilities.TryParseChannelMention(value, out var channelId) || ulong.TryParse(value, out channelId))
            {
                channel = context.Guild.GetChannel(channelId);
                if (channel is not null)
                {
                    var allowChannel = channelType switch
                    {
                        ChannelType.Text => channel.Type is ChannelType.Text or ChannelType.News or ChannelType.Store,
                        ChannelType.Voice => channel.Type is ChannelType.Voice or ChannelType.Stage,
                        _ => channel.Type == channelType || channelType is ChannelType.Unknown
                    };

                    if (allowChannel)
                        return ignoreChannelTypes is not null && ignoreChannelTypes.Contains(channel.Type)
                            ? TypeParserResult<DiscordChannel>.Failed(localization.GetText(context.Guild.Id, Localization.TypeParserChannelNotAllowed(channel.Type.ToString().ToLower())))
                            : TypeParserResult<DiscordChannel>.Successful(channel);
                }
            }
            else
            {
                channel = channelType switch
                {
                    ChannelType.Category => context.Guild.GetCategoryChannel(value),
                    ChannelType.Text => context.Guild.GetTextChannel(value),
                    ChannelType.Voice => context.Guild.GetVoiceChannel(value),
                    ChannelType.Unknown => ignoreChannelTypes is null
                        ? context.Guild.Channels
                            .OrderBy(c => c.Value.Position)
                            .FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase))
                            .Value
                        : context.Guild.Channels
                            .Where(c => !ignoreChannelTypes.Contains(c.Value.Type))
                            .OrderBy(c => c.Value.Position)
                            .FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase))
                            .Value
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