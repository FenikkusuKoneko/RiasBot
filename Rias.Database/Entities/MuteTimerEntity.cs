using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class MuteTimerEntity : DbEntity
{
    public ulong GuildId { get; set; }
        
    public ulong UserId { get; set; }
        
    public ulong ModeratorId { get; set; }
        
    public ulong MuteChannelSourceId { get; set; }
        
    public DateTime Expiration { get; set; }
}

public class MuteTimerEntityTypeConfiguration : IEntityTypeConfiguration<MuteTimerEntity>
{
    public void Configure(EntityTypeBuilder<MuteTimerEntity> builder)
    {
        builder.HasIndex(mt => new { mt.GuildId, mt.UserId }).IsUnique();
    }
}