using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Extensions
{
    public static class IMessageExtension
    {
        ///<summary>
        ///Send confirmation embed message in current text channel.
        ///</summary>
        public static async Task<IUserMessage> SendConfirmationEmbed(this IMessageChannel channel, string description)
        {
            if (channel != null)
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithDescription(description);
                return await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            return null;
        }

        ///<summary>
        ///Send error embed message in current text channel.
        ///</summary>
        public static async Task<IUserMessage> SendErrorEmbed(this IMessageChannel channel, string description)
        {
            if (channel != null)
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription(description);
                return await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            return null;
        }
    }
}
