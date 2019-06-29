using System;

namespace Rias.Core.Database.Models
{
    public class Users : DbEntity
    {
        public ulong UserId { get; set; }
        public int Currency { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
        public DateTime MessageDateTime { get; set; }
        public bool IsBlacklisted { get; set; }
        public bool IsBanned { get; set; }
    }
}