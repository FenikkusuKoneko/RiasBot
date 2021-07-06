using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
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

            var isValid = false;

            if (Contexts.HasFlag(ContextType.Guild))
            {
                isValid = context.Channel.Type == ChannelType.Category
                    || context.Channel.Type == ChannelType.Text
                    || context.Channel.Type == ChannelType.Voice
                    || context.Channel.Type == ChannelType.News;
            }

            if (Contexts.HasFlag(ContextType.DM))
                isValid = isValid || context.Channel.Type == ChannelType.Private;

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