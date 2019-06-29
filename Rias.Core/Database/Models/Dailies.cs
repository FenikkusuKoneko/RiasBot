using System;

namespace Rias.Core.Database.Models
{
    public class Dailies : DbEntity
    {
        public ulong UserId { get; set; }
        public DateTime NextDaily { get; set; }
    }
}