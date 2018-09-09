using Discord;
using System.Threading.Tasks;

namespace RiasBot.Extensions
{
    public static class MessageExtension
    {
        ///<summary>
        ///Send confirmation embed message in current text channel.
        ///</summary>
        public static async Task<IUserMessage> SendConfirmationMessageAsync(this IMessageChannel channel, string description)
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
        public static async Task<IUserMessage> SendErrorMessageAsync(this IMessageChannel channel, string description)
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
