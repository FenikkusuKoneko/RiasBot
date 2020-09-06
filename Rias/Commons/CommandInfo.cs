using System.Collections.Generic;

namespace Rias.Services
{
    public class CommandInfo
    {
        public string? Aliases { get; set; }
            
        public string? Description { get; set; }
            
        public IReadOnlyList<string>? Remarks { get; set; }
    }
}