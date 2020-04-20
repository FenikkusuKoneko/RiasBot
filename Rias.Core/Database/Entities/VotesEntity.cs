namespace Rias.Core.Database.Entities
{
    public class VotesEntity : DbEntity
    {
        public ulong UserId { get; set; }
        public string? Type { get; set; }
        public string? Query { get; set; }
        public bool IsWeekend { get; set; }
        public bool Checked { get; set; }
    }
}