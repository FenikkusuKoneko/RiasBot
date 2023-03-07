using Newtonsoft.Json;

namespace Rias.Common.Models;

public record JsonEmbed
{
    public string? Content { get; set; }
    
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

    public bool IsEmbedEmpty()
    {
        return Author is null
               && string.IsNullOrEmpty(Title)
               && string.IsNullOrEmpty(Description)
               && string.IsNullOrEmpty(Color)
               && string.IsNullOrEmpty(Thumbnail)
               && string.IsNullOrEmpty(Image)
               && (Fields is null || Fields.Length == 0)
               && Footer is null
               && Timestamp is null;
    }
        
    public record JsonEmbedAuthor
    {
        public string? Name { get; set; }
        
        public string? Url { get; set; }
        
        [JsonProperty("icon_url")]
        public string? IconUrl { get; set; }
    }

    public record JsonEmbedField
    {
        public string? Name { get; set; }
        
        public string? Value { get; set; }
        
        public bool Inline { get; set; }
    }

    public record JsonEmbedFooter
    {
        public string? Text { get; set; }
        
        [JsonProperty("icon_url")]
        public string? IconUrl { get; set; }
    }
}