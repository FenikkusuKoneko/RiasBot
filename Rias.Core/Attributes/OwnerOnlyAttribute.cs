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
        protected override async ValueTask<CheckResult> CheckAsync(RiasCommandContext context, IServiceProvider provider) 
            => context.User.Id == (await context.Client.GetApplicationInfoAsync()).Owner.Id
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(provider.GetRequiredService<Translations>().
                    GetText(context.Guild?.Id, null, "#attribute_owner_only"));
    }
}