namespace Rias.Database.Entities
{
    public class GuildXpRoleEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        
        public ulong RoleId { get; set; }
        
        public int Level { get; set; }
    }
}