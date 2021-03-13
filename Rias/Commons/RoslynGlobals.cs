using Rias.Implementation;

namespace Rias.Commons
{
    public class RoslynGlobals
    {
        public RiasBot? RiasBot { get; init; }
        
        public RiasCommandContext? Context { get; init; }
    }
}