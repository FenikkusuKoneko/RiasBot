using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Services.Database.Models
{
    public class SelfAssignableRoles : DbEntity
    {
        public ulong GuildId { get; set; }
        public string RoleName { get; set; }
        public ulong RoleId { get; set; }
    }
}
