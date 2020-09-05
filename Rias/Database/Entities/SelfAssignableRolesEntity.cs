namespace Rias.Database.Entities
{
    public class SelfAssignableRolesEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}