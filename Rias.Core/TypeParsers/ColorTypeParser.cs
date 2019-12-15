using System;
using System.Drawing;
using System.Threading.Tasks;
using Qmmands;
using Rias.Core.Implementation;
using Color = Discord.Color;

namespace Rias.Core.TypeParsers
{
    public class ColorTypeParser : RiasTypeParser<Color>
    {
        public override ValueTask<TypeParserResult<Color>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var hex = RiasUtils.HexToUint(value);
            if (hex.HasValue)
                return TypeParserResult<Color>.Successful(new Color(hex.Value));
            
            var color = default(System.Drawing.Color);
            if (Enum.TryParse<KnownColor>(value.Replace(" ", ""), true, out var knownColor))
                color = System.Drawing.Color.FromKnownColor(knownColor);

            if (!color.IsEmpty)
                return TypeParserResult<Color>.Successful(new Color(color.R, color.G, color.B));

            if (parameter.IsOptional)
                return TypeParserResult<Color>.Successful((Color) parameter.DefaultValue);
            
            return TypeParserResult<Color>.Unsuccessful("#TypeParser_InvalidColor");
        }
    }
}