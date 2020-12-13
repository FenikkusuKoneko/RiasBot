namespace Rias.Database.Entities
{
    public class WaifuEntity : DbEntity, IWaifuEntity
    {
        public CharacterEntity? Character { get; set; }
        
        public CustomCharacterEntity? CustomCharacter { get; set; }

        public ulong UserId { get; set; }
        
        public int? CharacterId { get; set; }
        
        public int? CustomCharacterId { get; set; }
        
        public string? Name => Character?.Name ?? CustomCharacter?.Name;

        public string? ImageUrl => Character?.ImageUrl ?? CustomCharacter?.ImageUrl;
        
        public string? CustomImageUrl { get; set; }
        
        public int Price { get; set; }
        
        public bool IsSpecial { get; set; }
        
        public int Position { get; set; }
    }
}