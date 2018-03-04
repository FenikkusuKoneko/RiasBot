using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Services.Database.Models
{
    public class Waifus : DbEntity
    {
        public ulong UserId { get; set; }
        public int WaifuId { get; set; }
        public string WaifuName { get; set; }
        public string WaifuUrl { get; set; }
        public string WaifuPicture { get; set; }
        public int WaifuPrice { get; set; }
        public bool IsPrimary { get; set; }
    }
}
