using System.Threading.Tasks;
using Discord;
using Rias.Core.Implementation;

namespace RiasBot.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, EmbedBuilder embed)
            => await channel.SendMessageAsync(embed: embed.Build());

        public static async Task<IUserMessage> SendConfirmationMessageAsync(this IMessageChannel channel, string message, string title = null, Color color = default)
            => await SendMessageAsync(channel, message, title, color != default ? color : RiasUtils.ConfirmColor);

        public static async Task<IUserMessage> SendErrorMessageAsync(this IMessageChannel channel, string message, string title = null, Color color = default)
            => await SendMessageAsync(channel, message, title, color != default ? color : RiasUtils.ErrorColor);

        private static async Task<IUserMessage> SendMessageAsync(IMessageChannel channel, string message, string title, Color color)
        {
            var embed = new EmbedBuilder
            {
                Color = color,
                Title = title,
                Description = message
            };

            return await channel.SendMessageAsync(embed: embed.Build());
        }
    }
}