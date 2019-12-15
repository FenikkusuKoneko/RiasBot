﻿namespace Rias.Core.Database.Models
{
    public class Votes : DbEntity
    {
        public ulong UserId { get; set; }
        public string? Type { get; set; }
        public string? Query { get; set; }
        public bool IsWeekend { get; set; }
        public bool Checked { get; set; }
    }
}