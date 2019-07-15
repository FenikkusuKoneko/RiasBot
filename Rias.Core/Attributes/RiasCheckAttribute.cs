using System;
using System.Threading.Tasks;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    public abstract class RiasCheckAttribute : CheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(CommandContext context, IServiceProvider provider)
            => CheckAsync((RiasCommandContext) context, provider);

        public abstract ValueTask<CheckResult> CheckAsync(RiasCommandContext context, IServiceProvider provider);
    }
}