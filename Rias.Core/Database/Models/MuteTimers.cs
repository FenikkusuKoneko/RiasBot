using System;

namespace Rias.Core.Database.Models
{
    public class MuteTimers : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public ulong ModeratorId { get; set; }
        public ulong MuteChannelSourceId { get; set; }
        public DateTime Expiration { get; set; }
    }
}