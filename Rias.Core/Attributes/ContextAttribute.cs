using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    public class ContextAttribute : RiasCheckAttribute
    {
        private readonly ContextType _contexts;

        public ContextAttribute(ContextType contexts)
        {
            _contexts = contexts;
        }
        
        protected override ValueTask<CheckResult> CheckAsync(RiasCommandContext context, IServiceProvider provider)
        {
            var tr = provider.GetRequiredService<Translations>();
            
            var isValid = false;

            if ((_contexts & ContextType.Guild) != 0)
                isValid = context.Channel is IGuildChannel;
            if ((_contexts & ContextType.DM) != 0)
                isValid = isValid || context.Channel is IDMChannel;
            if ((_contexts & ContextType.Group) != 0)
                isValid = isValid || context.Channel is IGroupChannel;

            var guildId = context.Guild?.Id;
            return isValid
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(tr.GetText(guildId, null, "#attribute_context",
                    _contexts.ToString().Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => tr.GetText(guildId, null, $"#common_{x.ToLower()}"))
                            .Humanize(x => $"**{x}**", tr.GetText(guildId, null, "#common_or").ToLowerInvariant())));
        }
    }
}