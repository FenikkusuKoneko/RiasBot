namespace Rias.Database.Entities
{
    public class CustomWaifuEntity : DbEntity, IWaifuEntity
    {
        public ulong UserId { get; set; }
        
        public string? Name { get; set; }
        
        public string? ImageUrl { get; set; }
        
        public bool IsSpecial { get; set; }
        
        public int Position { get; set; }
    }
}