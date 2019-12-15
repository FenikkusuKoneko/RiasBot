namespace Rias.Core.Database.Models
{
    public class Characters : DbEntity, ICharacter
    {
        public int CharacterId { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
    }
}