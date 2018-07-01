using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Services.Database.Models
{
    public class Patreon : DbEntity
    {
        public ulong UserId { get; set; }
        public int Reward { get; set; }
        public DateTime NextTimeReward { get; set; }
    }
}
