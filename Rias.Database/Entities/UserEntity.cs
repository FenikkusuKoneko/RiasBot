using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class UserEntity : DbEntity
{
    public Snowflake UserId { get; set; }
    public int Currency { get; set; }
    public int Xp { get; set; }
    public DateTime LastMessageDate { get; set; }
    public bool IsBanned { get; set; }
    public DateTime DailyTaken { get; set; }
}

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.HasIndex(u => u.UserId).IsUnique();
    }
}