using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Services.Database.Models
{
    public class UserConfig : DbEntity
    {
        public ulong UserId { get; set; }
        public int Currency { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
        public DateTime MessageDateTime { get; set; }
    }
}
