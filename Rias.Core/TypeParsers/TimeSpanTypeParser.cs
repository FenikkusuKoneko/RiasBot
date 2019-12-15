using System;
using System.Threading.Tasks;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class TimeSpanTypeParser : RiasTypeParser<TimeSpan>
    {
        public override ValueTask<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var timespan = RiasUtils.ConvertToTimeSpan(value);

            if (timespan.HasValue)
                return TypeParserResult<TimeSpan>.Successful(timespan.Value);

            if (parameter.IsOptional)
                return TypeParserResult<TimeSpan>.Successful((TimeSpan) parameter.DefaultValue);

            return TypeParserResult<TimeSpan>.Unsuccessful("#TypeParser_TimeSpanUnsuccessful");
        }
    }
}