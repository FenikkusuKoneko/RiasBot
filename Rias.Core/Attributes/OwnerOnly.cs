using System;
using System.Threading.Tasks;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    public class OwnerOnly : CheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(CommandContext context, IServiceProvider provider)
        {
            var riasContext = (RiasCommandContext) context;
            var user = riasContext.User;
            
            return user.Id == (await riasContext.Client.GetApplicationInfoAsync()).Owner.Id ? CheckResult.Successful : CheckResult.Unsuccessful("#attribute_owner_only");
        }
    }
}