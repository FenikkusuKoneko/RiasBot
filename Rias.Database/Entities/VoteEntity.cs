using Disqord;

namespace Rias.Database.Entities;

public class VoteEntity : DbEntity
{
    public Snowflake UserId { get; set; }
    public bool IsWeekend { get; set; }
    public bool Checked { get; set; }
}