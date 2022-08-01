using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class GuildXpRoleEntity : DbEntity
{
    public ulong GuildId { get; set; }
        
    public ulong RoleId { get; set; }
        
    public int Level { get; set; }
}

public class GuildXpRoleEntityTypeConfiguration : IEntityTypeConfiguration<GuildXpRoleEntity>
{
    public void Configure(EntityTypeBuilder<GuildXpRoleEntity> builder)
    {
        builder.HasIndex(gxpr => new { gxpr.GuildId, gxpr.RoleId }).IsUnique();
    }
}