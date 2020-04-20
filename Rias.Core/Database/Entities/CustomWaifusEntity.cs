namespace Rias.Core.Database.Entities
{
    public class CustomWaifusEntity : DbEntity, IWaifusEntity
    {
        public ulong UserId { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSpecial { get; set; }
        public int Position { get; set; }
    }
}