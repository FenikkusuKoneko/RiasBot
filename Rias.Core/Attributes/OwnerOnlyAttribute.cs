using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    /// <summary>
    ///     Requires that the user invoking the command to be the bot owner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class OwnerOnlyAttribute : RiasCheckAttribute
    {
        protected override async ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
            => context.User.Id == (await context.Client.GetApplicationInfoAsync()).Owner.Id
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(context.ServiceProvider.GetRequiredService<Resources>().GetText(context.Guild?.Id, "Attribute", "OwnerOnly"));
    }
}