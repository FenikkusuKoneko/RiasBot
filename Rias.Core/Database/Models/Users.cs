using System;

namespace Rias.Core.Database.Models
{
    public class Users : DbEntity
    {
        public ulong UserId { get; set; }
        public int Currency { get; set; }
        public int Xp { get; set; }
        public DateTime LastMessageDate { get; set; }
        public bool IsBlacklisted { get; set; }
        public bool IsBanned { get; set; }
        public DateTime DailyTaken { get; set; }
    }
}