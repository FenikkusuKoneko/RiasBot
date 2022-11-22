using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class SelfAssignableRoleEntity : DbEntity
{
    public Snowflake GuildId { get; set; }
    public Snowflake RoleId { get; set; }
}

public class SelfAssignableRoleEntityTypeConfiguration : IEntityTypeConfiguration<SelfAssignableRoleEntity>
{
    public void Configure(EntityTypeBuilder<SelfAssignableRoleEntity> builder)
    {
        builder.HasIndex(sar => new { sar.GuildId, sar.RoleId }).IsUnique();
    }
}