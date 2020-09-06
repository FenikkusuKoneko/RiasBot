using System.Threading.Tasks;
using Qmmands;
using Rias.Implementation;

namespace Rias.Attributes
{
    public abstract class RiasCheckAttribute : CheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(CommandContext context)
            => CheckAsync((RiasCommandContext)context);

        public abstract ValueTask<CheckResult> CheckAsync(RiasCommandContext context);
    }
}