using System;

namespace Rias.Core.Database.Models
{
    public class XpSystem : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
        public DateTime MessageDateTime { get; set; }
    }
}