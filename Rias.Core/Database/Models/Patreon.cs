using System;

namespace Rias.Core.Database.Models
{
    public class Patreon : DbEntity
    {
        public ulong UserId { get; set; }
        public int Reward { get; set; }
        public DateTime NextTimeReward { get; set; }
    }
}