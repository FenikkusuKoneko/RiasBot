using Disqord;
using Disqord.Gateway;
using Disqord.Rest;

namespace Rias.Services.Extensions;

public static class GuildExtensions
{
    public static CachedMember GetCurrentMember(this CachedGuild guild)
        => guild.GetMember(guild.Client.CurrentApplicationId) ?? throw new InvalidOperationException($"The current member is not cached in the guild ({guild.Id})");

    public static async Task<IWebhook?> GetWebhookAsync(this IGuild guild, Snowflake id)
        => (await guild.FetchWebhooksAsync()).FirstOrDefault(w => w.Id == id);
    
    public static int GetEmojiSlots(this CachedGuild guild)
    {
        return guild.BoostTier switch
        {
            GuildBoostTier.First => 100,
            GuildBoostTier.Second => 150,
            GuildBoostTier.Third => 250,
            _ => 50 // GuildBoostTier.None
        };
    }
}