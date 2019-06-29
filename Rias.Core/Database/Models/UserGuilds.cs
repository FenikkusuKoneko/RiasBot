using System;

namespace Rias.Core.Database.Models
{
    public class UserGuilds : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public bool IsMuted { get; set; }
        public DateTime MuteUntil { get; set; }
    }
}