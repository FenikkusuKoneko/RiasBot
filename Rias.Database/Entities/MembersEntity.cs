using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class MembersEntity : DbEntity
{
    public Snowflake GuildId { get; set; }
    public Snowflake MemberId { get; set; }
    public int Xp { get; set; }
    public DateTime LastMessageDate { get; set; }
    public bool IsMuted { get; set; }
    public bool IsXpIgnored { get; set; }
}

public class MembersEntityTypeConfiguration : IEntityTypeConfiguration<MembersEntity>
{
    public void Configure(EntityTypeBuilder<MembersEntity> builder)
    {
        builder.HasIndex(m => new { m.GuildId, m.MemberId }).IsUnique();
    }
}