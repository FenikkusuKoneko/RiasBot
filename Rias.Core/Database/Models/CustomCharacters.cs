namespace Rias.Core.Database.Models
{
    public class CustomCharacters : DbEntity, ICharacter
    {
        public int CharacterId { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}