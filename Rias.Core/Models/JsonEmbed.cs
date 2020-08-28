using System;
using Newtonsoft.Json;

namespace Rias.Core.Models
{
    public class JsonEmbed
    {
        public JsonEmbedAuthor? Author { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Thumbnail { get; set; }
        public string? Image { get; set; }
        public JsonEmbedField[]? Fields { get; set; }
        public JsonEmbedFooter? Footer { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public bool WithCurrentTimestamp { get; set; }
    }

    public class JsonEmbedAuthor
    {
        public string? Name { get; set; }
        public string? Url { get; set; }

        [JsonProperty("icon_url")]
        public string? IconUrl { get; set; }
    }

    public class JsonEmbedField
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public bool Inline { get; set; }
    }

    public class JsonEmbedFooter
    {
        public string? Text { get; set; }
        public string? IconUrl { get; set; }
    }
}