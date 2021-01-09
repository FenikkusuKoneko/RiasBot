namespace Rias.Services.Commons
{
    public class CharacterContent : AniListContent
    {
        public CharacterName Name { get; set; }
        
        public int? Favourites { get; set; }
        
        public AnimeMedia Media { get; set; }
        
        public string? Description { get; set; }
        
        public CharacterImage Image { get; set; }
    }
}