using System;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace Rias.Core.Extensions
{
    public static class GuildExtensions
    {
        public static int GetGuildEmotesSlots(this SocketGuild guild)
        {
            return guild.PremiumTier switch
            {
                PremiumTier.Tier1 => 100,
                PremiumTier.Tier2 => 150,
                PremiumTier.Tier3 => 250,
                _ => 50    //default is PremiumTier.None
            };
        }

        /// <summary>
        /// Gets a category channel by id or name (ordinal ignore case).
        /// </summary>
        public static SocketCategoryChannel? GetCategoryChannel(this SocketGuild guild, string value)
            => ulong.TryParse(value, out var channelId)
                ? guild.CategoryChannels.FirstOrDefault(x => x.Id == channelId)
                : guild.CategoryChannels.FirstOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets a text channel by mention, id or name (ordinal ignore case).
        /// </summary>
        public static SocketTextChannel? GetTextChannel(this SocketGuild guild, string value)
        {
            if (MentionUtils.TryParseChannel(value, out var channelId))
                return guild.GetTextChannel(channelId);

            return ulong.TryParse(value, out channelId)
                ? guild.GetTextChannel(channelId)
                : guild.TextChannels.FirstOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a voice channel by id or name (ordinal ignore case)
        /// </summary>
        public static SocketVoiceChannel? GetVoiceChannel(this SocketGuild guild, string value)
            => ulong.TryParse(value, out var channelId)
                ? guild.GetVoiceChannel(channelId)
                : guild.VoiceChannels.FirstOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));
        
        public static string GetRealIconUrl(this SocketGuild guild)
        {
            var iconId = guild.IconId;
            return $"https://cdn.discordapp.com/icons/416492045859946507/{iconId}{(iconId.StartsWith("a_") ? ".gif" : ".png")}?size=2048";
        }
        
        public static string GetBannerUrl(this SocketGuild guild)
        {
            return $"{guild.BannerUrl}?size=2048";
        }
        
        public static string GetSplashUrl(this SocketGuild guild)
        {
            return $"{guild.SplashUrl}?size=2048";
        }
    }
}