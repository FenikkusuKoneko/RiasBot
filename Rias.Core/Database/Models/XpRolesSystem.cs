namespace Rias.Core.Database.Models
{
    public class XpRolesSystem : DbEntity
    {
        public ulong GuildId { get; set; }
        public int Level { get; set; }
        public ulong RoleId { get; set; }
    }
}