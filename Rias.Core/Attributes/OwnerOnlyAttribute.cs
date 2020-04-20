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
        public override async ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            var riasBot = context.ServiceProvider.GetRequiredService<Rias>();
            var currentApplication = riasBot.CurrentApplication.IsFetched
                ? riasBot.CurrentApplication.Value
                : await riasBot.GetCurrentApplicationAsync();
            
            return context.User.Id == currentApplication.Owner.Id
                ? CheckResult.Successful
                : CheckResult.Unsuccessful(context.ServiceProvider.GetRequiredService<Localization>().GetText(context.Guild?.Id, Localization.AttributeOwnerOnly));
        }
    }
}