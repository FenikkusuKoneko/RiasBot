using System;

namespace Rias.Core.Database.Entities
{
    public class GuildsXpEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int Xp { get; set; }
        public DateTime LastMessageDate { get; set; }
    }
}