using System.Threading.Tasks;
using Discord;
using Rias.Core.Implementation;

namespace RiasBot.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<IUserMessage> SendConfirmationMessageAsync(this IMessageChannel channel, string message, string title = null, uint color = 0)
            => await SendMessageAsync(channel, message, title, color > 0 ? color : RiasUtils.ConfirmColor);

        public static async Task<IUserMessage> SendErrorMessageAsync(this IMessageChannel channel, string message, string title = null, uint color = 0)
            => await SendMessageAsync(channel, message, title, color > 0 ? color : RiasUtils.ErrorColor);

        private static async Task<IUserMessage> SendMessageAsync(IMessageChannel channel, string message, string title = null, uint color = 0)
        {
            var embed = new EmbedBuilder()
                .WithColor(color)
                .WithDescription(message);
            if (title != null)
                embed.WithTitle(title);
            return await channel.SendMessageAsync(embed: embed.Build());
        }
    }
}