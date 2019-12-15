namespace Rias.Core.Database.Models
{
    public class GuildUsers : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public bool IsMuted { get; set; }
    }
}