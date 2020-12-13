using System;

namespace Rias.Database.Entities
{
    public class GuildUserEntity : DbEntity
    {
        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }

        public int Xp { get; set; }

        public DateTime LastMessageDate { get; set; }
        
        public bool IsMuted { get; set; }
    }
}