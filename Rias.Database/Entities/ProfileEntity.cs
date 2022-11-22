using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class ProfileEntity : DbEntity
{
    public Snowflake UserId { get; set; }
    public string? BackgroundUrl { get; set; }
    public int BackgroundDim { get; set; }
    public string? Biography { get; set; }
    public string? Color { get; set; }
    public string[]? Badges { get; set; }
}

public class ProfileEntityTypeConfiguration : IEntityTypeConfiguration<ProfileEntity>
{
    public void Configure(EntityTypeBuilder<ProfileEntity> builder)
    {
        builder.HasIndex(p => p.UserId).IsUnique();
    }
}