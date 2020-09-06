using DSharpPlus.Entities;

namespace Rias.Models
{
    public class CustomMessage
    {
        public string? Content { get; set; }
        
        public DiscordEmbedBuilder? Embed { get; set; }
    }
}