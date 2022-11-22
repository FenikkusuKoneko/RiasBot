using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rias.Database.Enums;

namespace Rias.Database.Entities;

public class PatreonEntity : DbEntity
{
    public Snowflake UserId { get; set; }
    public int PatreonUserId { get; set; }
    public string? PatreonUserName { get; set; }
    public int AmountCents { get; set; }
    public int WillPayAmountCents { get; set; }
    public DateTimeOffset? LastChargeDate { get; set; }
    public LastChargeStatus? LastChargeStatus { get; set; }
    public PatronStatus? PatronStatus { get; set; }
    public int Tier { get; set; }
    public int TierAmountCents { get; set; }
    public bool Checked { get; set; }
}

public class PatreonEntityTypeConfiguration : IEntityTypeConfiguration<PatreonEntity>
{
    public void Configure(EntityTypeBuilder<PatreonEntity> builder)
    {
        builder.HasIndex(p => new { p.PatreonUserId, p.UserId }).IsUnique();
    }
}