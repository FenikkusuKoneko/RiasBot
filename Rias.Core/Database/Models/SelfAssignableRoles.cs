namespace Rias.Core.Database.Models
{
    public class SelfAssignableRoles : DbEntity
    {
        public ulong GuildId { get; set; }
        public string RoleName { get; set; }
        public ulong RoleId { get; set; }
    }
}