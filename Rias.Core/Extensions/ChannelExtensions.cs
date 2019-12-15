using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Rias.Core.Implementation;

namespace Rias.Core.Extensions
{
    public static class ChannelExtensions
    {
        public static async Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, EmbedBuilder embed)
            => await channel.SendMessageAsync(embed: embed.Build());

        public static async Task<IUserMessage> SendConfirmationMessageAsync(this IMessageChannel channel, string message, string? title = null)
            => await SendMessageAsync(channel, message, title, RiasUtils.ConfirmColor);

        public static async Task<IUserMessage> SendErrorMessageAsync(this IMessageChannel channel, string message, string? title = null)
            => await SendMessageAsync(channel, message, title, RiasUtils.ErrorColor);

        private static async Task<IUserMessage> SendMessageAsync(IMessageChannel channel, string message, string? title, Color color)
        {
            var embed = new EmbedBuilder
            {
                Color = color,
                Title = title,
                Description = message
            };

            return await channel.SendMessageAsync(embed: embed.Build());
        }

        public static bool CheckViewChannelPermission(SocketGuildUser bot, SocketGuildChannel channel)
        {
            var permissions = bot.GetPermissions(channel);
            return permissions.ViewChannel;
        }
    }
}