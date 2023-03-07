namespace Rias.Common;

/// <summary>
/// Common options from appsettings.json.
/// </summary>
public record RiasConfiguration
{
    public string[]? Prefixes { get; set; }
    public ulong LogsServerId { get; set; }
    public ulong LogsChannelId { get; set; }
    public string? CurrencyEmoji { get; set; }
    
    public string? InviteLink { get; set; }
    public string? OwnerServerInviteLink { get; set; }
    public ulong OwnerServerId { get; set; }
    
    public string? WebsiteUrl { get; set; }
    public string? PatreonUrl { get; set; }
    public string? DiscordBotListVoteUrl { get; set; }
    
    public string? UrbanDictionaryApiKey { get; set; }
    public string? ExchangeRateAccessKey { get; set; }
    public string? CoinMarketCapApiKey { get; set; }
    public string? WeebServicesToken { get; set; }
}