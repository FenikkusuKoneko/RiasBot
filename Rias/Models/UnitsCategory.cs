using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rias.Models
{
    #nullable disable
    public class UnitsCategory
    {
        public string Name { get; set; }
        public IEnumerable<Unit> Units { get; set; }
    }

    public class Unit
    {
        [JsonIgnore]
        public UnitsCategory Category { get; set; }
        
        public UnitName Name { get; set; }
        
        [JsonProperty("func_to_base")]
        public string FuncToBase { get; set; }
        
        [JsonProperty("func_from_base")]
        public string FuncFromBase { get; set; }
    }
    
    public struct UnitName
    {
        public string Singular { get; set; }
        public string Plural { get; set; }
        public IEnumerable<string> Abbreviations { get; set; }
    }
    #nullable enable
}