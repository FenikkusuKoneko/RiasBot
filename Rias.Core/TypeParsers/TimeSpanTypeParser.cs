using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.TypeParsers
{
    public class TimeSpanTypeParser : RiasTypeParser<TimeSpan>
    {
        public override ValueTask<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string value, RiasCommandContext context)
        {
            var timespan = RiasUtilities.ConvertToTimeSpan(value);

            if (timespan.HasValue)
                return TypeParserResult<TimeSpan>.Successful(timespan.Value);

            if (parameter.IsOptional)
                return TypeParserResult<TimeSpan>.Successful((TimeSpan) parameter.DefaultValue);

            var localization = context.ServiceProvider.GetRequiredService<Localization>();
            return TypeParserResult<TimeSpan>.Unsuccessful(localization.GetText(context.Guild?.Id, Localization.TypeParserTimeSpanUnsuccessful));
        }
    }
}