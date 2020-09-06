using Newtonsoft.Json;

namespace Rias.Services.Commons
{
    [JsonObject(Title = "Media")]
    public class AnimeMangaContent : AniListContent
    {
        public TitleInfo Title { get; set; }
        
        public string? Format { get; set; }
        
        public string? Type { get; set; }

        // Anime
        public int? Episodes { get; set; }
        
        public int? Duration { get; set; }

        // Manga
        public int? Chapters { get; set; }
        
        public int? Volumes { get; set; }

        public string? Status { get; set; }

        public Date StartDate { get; set; }
        
        public Date EndDate { get; set; }
        
        public string? Season { get; set; }
        
        public int? AverageScore { get; set; }
        
        public int? MeanScore { get; set; }
        
        public int? Popularity { get; set; }
        
        public int? Favourites { get; set; }
        
        public string? Source { get; set; }
        
        public string[]? Genres { get; set; }
        
        public string[]? Synonyms { get; set; }
        
        public bool IsAdult { get; set; }
        
        public string? Description { get; set; }
        
        public CoverImageInfo CoverImage { get; set; }
    }
}