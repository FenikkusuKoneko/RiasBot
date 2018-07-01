using System;
using System.Collections.Generic;

namespace RiasBot.Services.Database.Models
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }
        public ulong MuteRole { get; set; }
        public bool Greet { get; set; }
        public string GreetMessage { get; set; }
        public ulong GreetChannel { get; set; }
        public bool Bye { get; set; }
        public string ByeMessage { get; set; }
        public ulong ByeChannel { get; set; }
        public bool XpGuildNotification { get; set; }
        public ulong AutoAssignableRole { get; set; }
        public int WarnsPunishment { get; set; }
        public string PunishmentMethod { get; set; }
        public ulong ModLogChannel { get; set; }
    }
}
