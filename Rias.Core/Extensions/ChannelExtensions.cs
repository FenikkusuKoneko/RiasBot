using System.Threading.Tasks;
using Disqord;
using Rias.Core.Implementation;

namespace Rias.Core.Extensions
{
    public static class ChannelExtensions
    {
        public static async Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, LocalEmbedBuilder embed)
            => await channel.SendMessageAsync(embed: embed.Build());

        public static async Task<IUserMessage> SendConfirmationMessageAsync(this IMessageChannel channel, string message, string? title = null)
            => await SendMessageAsync(channel, message, title, RiasUtilities.ConfirmColor);

        public static async Task<IUserMessage> SendErrorMessageAsync(this IMessageChannel channel, string message, string? title = null)
            => await SendMessageAsync(channel, message, title, RiasUtilities.ErrorColor);

        private static async Task<IUserMessage> SendMessageAsync(IMessageChannel channel, string message, string? title, Color color)
        {
            var embed = new LocalEmbedBuilder()
            {
                Color = color,
                Title = title,
                Description = message
            };

            return await channel.SendMessageAsync(embed: embed.Build());
        }

        public static bool CheckViewChannelPermission(CachedMember bot, CachedGuildChannel channel)
        {
            var permissions = bot.GetPermissionsFor(channel);
            return permissions.ViewChannel;
        }
    }
}