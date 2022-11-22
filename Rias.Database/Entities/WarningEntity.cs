using Disqord;

namespace Rias.Database.Entities;

public class WarningEntity : DbEntity
{
    public Snowflake GuildId { get; set; }
    public Snowflake UserId { get; set; }
    public string? Reason { get; set; }
    public Snowflake ModeratorId { get; set; }
}