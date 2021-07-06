using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Rias.Implementation;

namespace Rias.Extensions
{
    public static class GuildExtensions
    {
        public static int GetGuildEmotesSlots(this DiscordGuild guild)
        {
            return guild.PremiumTier switch
            {
                PremiumTier.Tier_1 => 100,
                PremiumTier.Tier_2 => 150,
                PremiumTier.Tier_3 => 250,
                _ => 50 // default is PremiumTier.None
            };
        }

        /// <summary>
        /// Gets a category channel by id or name (ordinal ignore case).
        /// </summary>
        public static DiscordChannel? GetCategoryChannel(this DiscordGuild guild, string value)
            => ulong.TryParse(value, out var channelId)
                ? guild.Channels.Where(x => x.Value.Type == ChannelType.Category)
                    .FirstOrDefault(x => x.Key == channelId).Value
                : guild.Channels.Where(x => x.Value.Type == ChannelType.Category)
                    .FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;

        /// <summary>
        /// Gets a text channel by mention, id or name (ordinal ignore case).
        /// </summary>
        public static DiscordChannel? GetTextChannel(this DiscordGuild guild, string value)
        {
            if (RiasUtilities.TryParseChannelMention(value, out var channelId))
                return guild.GetChannel(channelId);

            return ulong.TryParse(value, out channelId)
                ? guild.GetChannel(channelId)
                : guild.Channels.Where(x => x.Value.Type is ChannelType.Text or ChannelType.News or ChannelType.Store)
                    .FirstOrDefault(x => string.Equals(x.Value.Name, value.Replace(' ', '-'), StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>
        /// Gets a voice channel by id or name (ordinal ignore case).
        /// </summary>
        public static DiscordChannel? GetVoiceChannel(this DiscordGuild guild, string value)
            => ulong.TryParse(value, out var channelId)
                ? guild.GetChannel(channelId)
                : guild.Channels.Where(x => x.Value.Type == ChannelType.Voice)
                    .FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;

        public static async Task<DiscordWebhook?> GetWebhookAsync(this DiscordGuild guild, ulong id)
            => (await guild.GetWebhooksAsync())?.FirstOrDefault(x => x.Id == id);

        public static string GetIconUrl(this DiscordGuild guild)
            => $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconHash}.{(guild.IconHash.StartsWith("a_") ? "gif" : "png")}?size=2048";
    }
}