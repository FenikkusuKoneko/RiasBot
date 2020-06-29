namespace Rias.Core.Database.Entities
{
    public class SelfAssignableRolesEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}