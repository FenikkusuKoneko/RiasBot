namespace Rias.Database.Entities
{
    public class WarningEntity : DbEntity
    {
        public ulong GuildId { get; set; }
        
        public ulong UserId { get; set; }
        
        public string? Reason { get; set; }
        
        public ulong ModeratorId { get; set; }
    }
}