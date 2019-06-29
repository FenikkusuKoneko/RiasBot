using System;

namespace Rias.Core.Database.Models
{
    public class MuteTimers : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public ulong Moderator { get; set; }
        public ulong MuteChannelSource { get; set; }
        public DateTime MutedUntil { get; set; }
    }
}