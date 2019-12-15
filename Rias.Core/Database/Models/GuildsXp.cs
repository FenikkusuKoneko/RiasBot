using System;

namespace Rias.Core.Database.Models
{
    public class GuildsXp : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int Xp { get; set; }
        public DateTime LastMessageDate { get; set; }
    }
}