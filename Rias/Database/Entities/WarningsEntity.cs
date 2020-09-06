namespace Rias.Database.Entities
{
    public class WarningsEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        
        public ulong UserId { get; set; }
        
        public string? Reason { get; set; }
        
        public ulong ModeratorId { get; set; }
    }
}