using System;
using System.Threading.Tasks;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    public class OwnerOnlyAttribute : RiasCheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(RiasCommandContext context, IServiceProvider provider) 
            => context.User.Id == (await context.Client.GetApplicationInfoAsync()).Owner.Id
                ? CheckResult.Successful
                : CheckResult.Unsuccessful("#attribute_owner_only");
    }
}