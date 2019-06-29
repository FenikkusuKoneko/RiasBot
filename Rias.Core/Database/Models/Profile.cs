namespace Rias.Core.Database.Models
{
    public class Profile : DbEntity
    {
        public ulong UserId { get; set; }
        public string BackgroundUrl { get; set; }
        public int BackgroundDim { get; set; }
        public ulong MarriedUser { get; set; }
        public string Bio { get; set; }
    }
}