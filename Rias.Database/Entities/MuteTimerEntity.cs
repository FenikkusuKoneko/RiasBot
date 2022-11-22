using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class MuteTimerEntity : DbEntity
{
    public Snowflake GuildId { get; set; }
    public Snowflake UserId { get; set; }
    public Snowflake ModeratorId { get; set; }
    public Snowflake MuteChannelSourceId { get; set; }
    public DateTime Expiration { get; set; }
}

public class MuteTimerEntityTypeConfiguration : IEntityTypeConfiguration<MuteTimerEntity>
{
    public void Configure(EntityTypeBuilder<MuteTimerEntity> builder)
    {
        builder.HasIndex(mt => new { mt.GuildId, mt.UserId }).IsUnique();
    }
}