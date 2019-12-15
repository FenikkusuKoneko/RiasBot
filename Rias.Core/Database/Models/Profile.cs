namespace Rias.Core.Database.Models
{
    public class Profile : DbEntity
    {
        public ulong UserId { get; set; }
        public string? BackgroundUrl { get; set; }
        public int BackgroundDim { get; set; }
        public string? Biography { get; set; }
        public string? Color { get; set; }
        public string[]? Badges { get; set; }
    }
}