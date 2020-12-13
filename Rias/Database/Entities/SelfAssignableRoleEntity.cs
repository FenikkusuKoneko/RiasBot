namespace Rias.Database.Entities
{
    public class SelfAssignableRoleEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        
        public ulong RoleId { get; set; }
    }
}