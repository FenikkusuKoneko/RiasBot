using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    /// <summary>
    ///     Requires that the user invoking the command to be the bot owner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class OwnerOnlyAttribute : RiasCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            return context.Client.CurrentApplication.Owners.Any(x => x.Id == context.User.Id)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(context.ServiceProvider.GetRequiredService<Localization>().GetText(context.Guild?.Id, Localization.AttributeOwnerOnly));
        }
    }
}