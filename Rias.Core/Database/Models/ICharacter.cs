namespace Rias.Core.Database.Models
{
    public interface ICharacter
    {
        public int CharacterId { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
    }
}