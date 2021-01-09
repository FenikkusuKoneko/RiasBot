namespace Rias.Services.Commons
{
    public class AniListContent
    {
        public int Id { get; set; }
        
        public string? SiteUrl { get; set; }

        public struct TitleInfo
        {
            public string? Romaji { get; set; }
            
            public string? English { get; set; }
            
            public string? Native { get; set; }
        }

        public struct Date
        {
            public int? Year { get; set; }
            
            public int? Month { get; set; }
            
            public int? Day { get; set; }
        }

        public struct CoverImageInfo
        {
            public string? Large { get; set; }
        }

        public struct CharacterName
        {
            public string? First { get; set; }
            
            public string? Last { get; set; }
            
            public string? Full { get; set; }
            
            public string? Native { get; set; }
            
            public string[]? Alternative { get; set; }
        }
        
        public struct CharacterImage
        {
            public string? Large { get; set; }
        }

        public struct AnimeMedia
        {
            public AnimeMangaContent[]? Nodes { get; set; }
        }
        
        public struct CharacterMedia
        {
            public CharacterContent[] Nodes { get; set; }
        }
    }
}