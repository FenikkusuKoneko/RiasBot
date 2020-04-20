namespace Rias.Core.Database.Entities
{
    public class CharactersEntity : DbEntity, ICharacterEntity
    {
        public int CharacterId { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? ImageUrl { get; set; }
    }
}