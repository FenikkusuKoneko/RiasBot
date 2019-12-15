namespace Rias.Core.Database.Models
{
    public class CustomWaifus : DbEntity, IWaifus
    {
        public ulong UserId { get; set; }
        public string? Name { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSpecial { get; set; }
        public int Position { get; set; }
    }
}