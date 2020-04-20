using System;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class ColorTypeParser : RiasTypeParser<Color>
    {
        public override ValueTask<TypeParserResult<Color>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var hex = RiasUtilities.HexToInt(value);
            if (hex.HasValue)
                return TypeParserResult<Color>.Successful(new Color(hex.Value));
            
            var color = default(System.Drawing.Color);
            if (Enum.TryParse<System.Drawing.KnownColor>(value.Replace(" ", ""), true, out var knownColor))
                color = System.Drawing.Color.FromKnownColor(knownColor);

            if (!color.IsEmpty)
                return TypeParserResult<Color>.Successful(new Color(color.R, color.G, color.B));

            if (parameter.IsOptional)
                return TypeParserResult<Color>.Successful((Color) parameter.DefaultValue);

            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            return TypeParserResult<Color>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserInvalidColor));
        }
    }
}