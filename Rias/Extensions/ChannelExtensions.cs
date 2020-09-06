using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Rias.Implementation;

namespace Rias.Extensions
{
    public static class ChannelExtensions
    {
        public static async Task<DiscordMessage> SendMessageAsync(this DiscordChannel channel, DiscordEmbedBuilder embed)
            => await channel.SendMessageAsync(embed: embed);

        public static async Task<DiscordMessage> SendConfirmationMessageAsync(this DiscordChannel channel, string message, string? title = null)
            => await SendMessageAsync(channel, message, title, RiasUtilities.ConfirmColor);

        public static async Task<DiscordMessage> SendErrorMessageAsync(this DiscordChannel channel, string message, string? title = null)
            => await SendMessageAsync(channel, message, title, RiasUtilities.ErrorColor);
        
        public static bool CheckViewChannelPermission(DiscordMember bot, DiscordChannel channel)
        {
            var permissions = bot.PermissionsIn(channel);
            return permissions.HasPermission(Permissions.AccessChannels);
        }

        private static async Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, string message, string? title, DiscordColor color)
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = color,
                Title = title,
                Description = message
            };

            return await channel.SendMessageAsync(embed: embed);
        }
    }
}