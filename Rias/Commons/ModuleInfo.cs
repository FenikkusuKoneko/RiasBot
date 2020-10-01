using System.Collections.Generic;

namespace Rias.Commons
{
    public class ModuleInfo
    {
        public string? Name { get; set; }
            
        public string? Aliases { get; set; }
            
        public IReadOnlyList<CommandInfo>? Commands { get; set; }
            
        public IReadOnlyList<ModuleInfo>? Submodules { get; set; }
    }
}