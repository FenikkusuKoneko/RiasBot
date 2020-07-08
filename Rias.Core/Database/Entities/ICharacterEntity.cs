namespace Rias.Core.Database.Entities
{
    public interface ICharacterEntity
    {
        public int CharacterId { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
    }
}