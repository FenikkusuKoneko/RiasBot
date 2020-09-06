using System.Collections.Generic;

namespace Rias.Models
{
#nullable disable
    public class UnitsCategory
    {
        public string Name { get; set; }
        
        public IEnumerable<Unit> Units { get; set; }
    }
#nullable enable
}