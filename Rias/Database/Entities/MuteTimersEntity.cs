using System;

namespace Rias.Database.Entities
{
    public class MuteTimersEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public ulong ModeratorId { get; set; }
        public ulong MuteChannelSourceId { get; set; }
        public DateTime Expiration { get; set; }
    }
}