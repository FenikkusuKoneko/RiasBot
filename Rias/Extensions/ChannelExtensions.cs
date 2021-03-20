using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Rias.Implementation;

namespace Rias.Extensions
{
    public static class ChannelExtensions
    {
        public static async Task<DiscordMessage> SendConfirmationMessageAsync(this DiscordChannel channel, string message)
            => await SendMessageAsync(channel, message, RiasUtilities.ConfirmColor);

        public static async Task<DiscordMessage> SendErrorMessageAsync(this DiscordChannel channel, string message)
            => await SendMessageAsync(channel, message, RiasUtilities.ErrorColor);
        
        public static bool CheckViewChannelPermission(DiscordMember bot, DiscordChannel channel)
        {
            var permissions = bot.PermissionsIn(channel);
            return permissions.HasPermission(Permissions.AccessChannels);
        }

        private static async Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, string message, DiscordColor color)
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = color,
                Description = message
            };

            return await channel.SendMessageAsync(embed);
        }
    }
}