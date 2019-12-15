using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
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

        protected override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            var resources = context.ServiceProvider.GetRequiredService<Resources>();

            var isValid = false;

            if ((_contexts & ContextType.Guild) != 0)
                isValid = context.Channel is SocketGuildChannel;
            if ((_contexts & ContextType.DM) != 0)
                isValid = isValid || context.Channel is SocketDMChannel;

            if (isValid)
                return CheckResult.Successful;

            var guildId = context.Guild?.Id;
            var contexts = _contexts.ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => resources.GetText(guildId, "Common", x));

            var contextsHumanized = contexts.Humanize(x => $"**{x}**", resources.GetText(guildId, "Common", "Or").ToLowerInvariant());
            return CheckResult.Unsuccessful(resources.GetText(guildId, "Attribute", "Context", contextsHumanized));
        }
    }
}