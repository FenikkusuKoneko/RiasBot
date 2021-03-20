using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Implementation;

namespace Rias.TypeParsers
{
    public class TimeSpanTypeParser : RiasTypeParser<TimeSpan>
    {
        public override ValueTask<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var timespan = RiasUtilities.ConvertToTimeSpan(value);

            if (timespan.HasValue)
                return TypeParserResult<TimeSpan>.Successful(timespan.Value);

            var localization = context.Services.GetRequiredService<Localization>();
            return TypeParserResult<TimeSpan>.Failed(localization.GetText(context.Guild?.Id, Localization.TypeParserTimeSpanUnsuccessful));
        }
    }
}