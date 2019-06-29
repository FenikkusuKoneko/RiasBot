using System.Threading.Tasks;
using Qmmands;

namespace Rias.Core.Modules.Utility
{
    [Name("Utility")]
    public class Utility : RiasModule
    {
        [Command("test")]
        public async Task TestAsync()
        {
            
        }
    }
}