using System;
using System.Linq;
using System.Threading.Tasks;
using Qmmands;
using Rias.Implementation;

namespace Rias.Attributes
{
    /// <summary>
    ///     Requires that the user invoking the command to be the bot owner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MasterOnlyAttribute : RiasCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            return context.Client.CurrentApplication.Owners.Any(x => x.Id == context.User.Id)
                ? CheckResult.Successful
                : CheckResult.Failed("This command can be used only by my master.");
        }
    }
}