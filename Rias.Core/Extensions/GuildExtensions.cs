using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;

namespace Rias.Core.Extensions
{
    public static class GuildExtensions
    {
        public static int GetGuildEmotesSlots(this CachedGuild guild)
        {
            return guild.BoostTier switch
            {
                BoostTier.First => 100,
                BoostTier.Second => 150,
                BoostTier.Third => 250,
                _ => 50    //default is PremiumTier.None
            };
        }

        /// <summary>
        /// Gets a category channel by id or name (ordinal ignore case).
        /// </summary>
        public static CachedCategoryChannel? GetCategoryChannel(this CachedGuild guild, string value)
            => ulong.TryParse(value, out var channelId)
                ? guild.CategoryChannels.FirstOrDefault(x => x.Key == channelId).Value
                : guild.CategoryChannels.FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;

        /// <summary>
        /// Gets a text channel by mention, id or name (ordinal ignore case).
        /// </summary>
        public static CachedTextChannel? GetTextChannel(this CachedGuild guild, string value)
        {
            if (Discord.TryParseChannelMention(value, out var channelSnowflake))
                return guild.GetTextChannel(channelSnowflake);

            return ulong.TryParse(value, out var channelId)
                ? guild.GetTextChannel(channelId)
                : guild.TextChannels.FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>
        /// Gets a voice channel by id or name (ordinal ignore case)
        /// </summary>
        public static CachedVoiceChannel? GetVoiceChannel(this CachedGuild guild, string value)
            => ulong.TryParse(value, out var channelId)
                ? guild.GetVoiceChannel(channelId)
                : guild.VoiceChannels.FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;

        public static string GetRealIconUrl(this CachedGuild guild)
            => guild.GetIconUrl(guild.IconHash.StartsWith("a_") ? ImageFormat.Gif : ImageFormat.Default);

        public static async Task<RestWebhook?> GetWebhookAsync(this CachedGuild guild, Snowflake id)
            => (await guild.GetWebhooksAsync()).FirstOrDefault(x => x.Id == id);
    }
}