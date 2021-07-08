using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Commons;
using Rias.Implementation;

namespace Rias.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ContextAttribute : RiasCheckAttribute
    {
        public ContextAttribute(ContextType contexts)
        {
            Contexts = contexts;
        }

        public ContextType Contexts { get; }

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            var localization = context.Services.GetRequiredService<Localization>();

            var isValid = Contexts switch
            {
                ContextType.Guild when context.Guild is not null => true,
                ContextType.DM when context.Guild is null => true,
                _ => false
            };

            if (isValid)
                return CheckResult.Successful;

            var guildId = context.Guild?.Id;
            var contexts = Contexts.ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => localization.GetText(guildId, Localization.CommonContextType(x.ToLower())));

            var contextsHumanized = contexts.Humanize(x => $"**{x}**", localization.GetText(guildId, Localization.CommonOr).ToLowerInvariant());
            return CheckResult.Failed(localization.GetText(guildId, Localization.AttributeContext, contextsHumanized));
        }
    }
}