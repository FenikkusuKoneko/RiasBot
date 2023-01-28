using Disqord;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qommon;
using Rias.Common;

namespace Rias.Services.TypeParsers;

public class GuildEmojiTypeParser : DiscordGuildTypeParser<IGuildEmoji>
{
    public override ValueTask<ITypeParserResult<IGuildEmoji>> ParseAsync(IDiscordGuildCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        var guild = context.Bot.GetGuild(context.GuildId);
        if (guild is null)
            throw new InvalidOperationException($"{nameof(GuildEmojiTypeParser)} requires the context guild to be cached.");

        var valueString = value.ToString();
        string? invalidEmojiString;
        
        if (LocalCustomEmoji.TryParse(value.Span, out var emoji))
        {
            if (guild.Emojis.TryGetValue(emoji.Id.Value, out var guildEmoji))
                return Success(new Optional<IGuildEmoji>(guildEmoji));
            
            invalidEmojiString = Strings.TypeParser.EmojiNotFromGuild;
        }
        else
        {
            var guildEmoji = guild.Emojis.Values.FirstOrDefault(e => string.Equals(e.Name, valueString, StringComparison.OrdinalIgnoreCase));
            
            if (guildEmoji is not null)
                return Success(new Optional<IGuildEmoji>(guildEmoji));

            invalidEmojiString = Strings.TypeParser.GuildEmojiNotFound;
        }
        
        var localisation = context.Services.GetRequiredService<LocalisationService>();
        return Failure(localisation.GetText(context.GuildId, invalidEmojiString, emoji?.Name ?? valueString));
    }
}