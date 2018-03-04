using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Services.Database.Models
{
    public class Shop : DbEntity
    {
        public ulong GuildId { get; set; }
        public string ItemName { get; set; }
        public int ItemPrice { get; set; }
        public string Type { get; set; }
    }
}
