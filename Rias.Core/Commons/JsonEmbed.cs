using System;
using Discord;

namespace Rias.Core.Commons
{
    public class JsonEmbed
    {
        public EmbedAuthorBuilder? Author { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Thumbnail { get; set; }
        public string? Image { get; set; }
        public EmbedFields[]? Fields { get; set; }
        public EmbedFooterBuilder? Footer { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public bool WithCurrentTimestamp { get; set; }
    }

    public class EmbedFields
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public bool Inline { get; set; }
    }
}