using Disqord;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qommon.Collections;
using Rias.Common;

namespace Rias.Services.TypeParsers;

// Taken from Disqord.Bot.Commands.Parsers.GuildChannelTypeParser and adapted to support localisation

/// <summary>
///     Represents type parsing for the <see cref="IGuildChannel"/> type and any derived types.
///     Does <b>not</b> support parsing channels that are not in the cache.
/// </summary>
/// <remarks>
///     Supports the following inputs, in order:
///     <list type="number">
///         <item>
///             <term> ID </term>
///             <description> The ID of the channel. </description>
///         </item>
///         <item>
///             <term> Mention </term>
///             <description> The mention of the channel. </description>
///         </item>
///         <item>
///             <term> Name </term>
///             <description> The name of the channel. This is case-insensitive. </description>
///         </item>
///     </list>
/// </remarks>
public class GuildChannelTypeParser<TChannel> : DiscordGuildTypeParser<TChannel>
    where TChannel : class, IGuildChannel
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly string ChannelNotFoundString;
    
    static GuildChannelTypeParser()
    {
        var type = typeof(TChannel);
        ChannelNotFoundString = type != typeof(IGuildChannel) && type.IsInterface
            ? Strings.TypeParser.ChannelNotFound(type.Name[1..type.Name.IndexOf("channel", StringComparison.OrdinalIgnoreCase)].Replace("guild", "", StringComparison.OrdinalIgnoreCase).ToLower())
            : Strings.TypeParser.ChannelNotFound();
    }

    public override ValueTask<ITypeParserResult<TChannel>> ParseAsync(IDiscordGuildCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        if (!context.Bot.CacheProvider.TryGetChannels(context.GuildId, out var channelCache))
            throw new InvalidOperationException($"The {GetType().Name} requires the channel cache.");

        // Wraps the cache in a pattern-matching wrapper dictionary.
        var channels = new ReadOnlyOfTypeDictionary<Snowflake, CachedGuildChannel, TChannel>(channelCache);
        TChannel? foundChannel = null;
        var valueSpan = value.Span;
        
        if (Snowflake.TryParse(valueSpan, out var id) || Mention.TryParseChannel(valueSpan, out id))
        {
            // The value is a mention or an id.
            foundChannel = channels.GetValueOrDefault(id);
        }
        else
        {
            // The value is possibly a name.
            foreach (var channel in channels.Values)
            {
                if (!valueSpan.Equals(channel.Name, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                foundChannel = channel;
                break;
            }
        }

        if (foundChannel != null)
            return Success(foundChannel);
        
        var localisation = context.Services.GetRequiredService<LocalisationService>();
        return Failure(localisation.GetText(context.GuildId, ChannelNotFoundString));
    }
}