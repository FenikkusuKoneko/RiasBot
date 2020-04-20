namespace Rias.Core.Database.Entities
{
    public class GuildUsersEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public bool IsMuted { get; set; }
    }
}