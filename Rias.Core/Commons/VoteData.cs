using Newtonsoft.Json;

namespace Rias.Core.Commons
{
    public class VoteData
    {
        [JsonProperty("bot")]
        public ulong Bot { get; set; }
        
        [JsonProperty("user")]
        public ulong User { get; set; }
        
        [JsonProperty("type")]
        public string? Type { get; set; }
        
        [JsonProperty("query")]
        public string? Query { get; set; }
        
        [JsonProperty("is_weekend")]
        public bool IsWeekend { get; set; }
    }
}