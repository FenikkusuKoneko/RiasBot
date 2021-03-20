using System;

namespace Rias.Database.Entities
{
    public class MembersEntity : DbEntity
    {
        public ulong GuildId { get; set; }

        public ulong MemberId { get; set; }

        public int Xp { get; set; }

        public DateTime LastMessageDate { get; set; }
        
        public bool IsMuted { get; set; }
        
        public bool IsXpIgnored { get; set; }
    }
}