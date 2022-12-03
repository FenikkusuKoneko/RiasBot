using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rias.Database.Entities;

public class GuildEntity : DbEntity
{
    public Snowflake GuildId { get; set; }
    public string? Prefix { get; set; }
    public Snowflake MuteRoleId { get; set; }
    public bool IsGreetEnabled { get; set; }
    public string? GreetMessage { get; set; }
    public Snowflake GreetWebhookId { get; set; }
    public bool IsByeEnabled { get; set; }
    public string? ByeMessage { get; set; }
    public Snowflake ByeWebhookId { get; set; }
    public bool IsXpNotificationEnabled { get; set; }
    public Snowflake XpWebhookId { get; set; }
    public string? XpLevelUpMessage { get; set; }
    public string? XpLevelUpRoleRewardMessage { get; set; }
    public Snowflake AutoAssignableRoleId { get; set; }
    public int RequiredPunishmentWarnings { get; set; }
    public string? WarningPunishment { get; set; }
    public Snowflake ModLogChannelId { get; set; }
    public string? Locale { get; set; }
    public ulong[]? XpIgnoredChannels { get; set; }
    public ulong XpIgnoredRoleId { get; set; }
}

public class GuildEntityTypeConfiguration : IEntityTypeConfiguration<GuildEntity>
{
    public void Configure(EntityTypeBuilder<GuildEntity> builder)
    {
        builder.HasIndex(g => g.GuildId).IsUnique();
    }
}