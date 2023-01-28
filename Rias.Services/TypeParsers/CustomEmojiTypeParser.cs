using Disqord;
using Disqord.Bot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;

namespace Rias.Services.TypeParsers;

public class CustomEmojiTypeParser : DiscordTypeParser<ICustomEmoji>
{
    public override ValueTask<ITypeParserResult<ICustomEmoji>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        if (LocalCustomEmoji.TryParse(value.Span, out var emoji))
            return Success(emoji);

        var localisation = context.Services.GetRequiredService<LocalisationService>();
        return Failure(localisation.GetText(context.GuildId, Strings.TypeParser.InvalidEmoji, value));
    }
}