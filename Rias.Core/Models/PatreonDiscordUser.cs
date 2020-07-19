using Newtonsoft.Json;

namespace Rias.Core.Models
{
#nullable disable
    public class PatreonDiscordUser
    {
        [JsonProperty("patreon_id")]
        public int PatreonId { get; set; }
        [JsonProperty("discord_id")]
        public ulong DiscordId { get; set; }
        
        [JsonProperty("patreon_username")]
        public string PatreonUsername { get; set; }
        [JsonProperty("discord_username")]
        public string DiscordUsername { get; set; }
        
        [JsonProperty("discord_discriminator")]
        public string DiscordDiscriminator { get; set; }
        
        [JsonProperty("discord_avatar")]
        public string DiscordAvatar { get; set; }
        
        [JsonProperty("tier")]
        public int Tier { get; set; }
    }
#nullable enable
}