namespace Rias.Core.Database.Entities
{
    public class GuildXpRolesEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
        public int Level { get; set; }
    }
}