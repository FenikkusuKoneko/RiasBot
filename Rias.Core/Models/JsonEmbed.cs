using System;
using Disqord;

namespace Rias.Core.Models
{
    public class JsonEmbed
    {
        public LocalEmbedAuthorBuilder? Author { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Thumbnail { get; set; }
        public string? Image { get; set; }
        public LocalEmbedFieldBuilder[]? Fields { get; set; }
        public LocalEmbedFooterBuilder? Footer { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public bool WithCurrentTimestamp { get; set; }
    }
}