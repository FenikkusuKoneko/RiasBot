using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Implementation;

namespace Rias.TypeParsers
{
    public class ColorTypeParser : RiasTypeParser<DiscordColor>
    {
        public override ValueTask<TypeParserResult<DiscordColor>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var hex = RiasUtilities.HexToInt(value);
            if (hex.HasValue)
                return TypeParserResult<DiscordColor>.Successful(new DiscordColor(hex.Value));
            
            var color = default(System.Drawing.Color);
            if (Enum.TryParse<System.Drawing.KnownColor>(value.Replace(" ", ""), true, out var knownColor))
                color = System.Drawing.Color.FromKnownColor(knownColor);

            if (!color.IsEmpty)
                return TypeParserResult<DiscordColor>.Successful(new DiscordColor(color.R, color.G, color.B));

            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            return TypeParserResult<DiscordColor>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserInvalidColor));
        }
    }
}