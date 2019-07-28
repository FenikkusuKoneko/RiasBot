using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Rias.Core.Extensions
{
    public static class ChannelExtensions
    {
        public static async Task<ICategoryChannel> GetCategoryByIdAsync(IGuild guild, string id)
        {
            return ulong.TryParse(id, out var categoryId)
                ? (await guild.GetCategoriesAsync()).FirstOrDefault(x => x.Id == categoryId)
                : null;
        }
        
        public static async Task<ITextChannel> GetTextChannelByMentionOrIdAsync(IGuild guild, string value)
        {
            if (MentionUtils.TryParseChannel(value, out var channelId))
                return (await guild.GetTextChannelsAsync()).FirstOrDefault(x => x.Id == channelId);
            return ulong.TryParse(value, out channelId) ? (await guild.GetTextChannelsAsync()).FirstOrDefault(x => x.Id == channelId) : null;
        }
            
        public static async Task<IVoiceChannel> GetVoiceChannelByIdAsync(IGuild guild, string id)
        {
            return ulong.TryParse(id, out var channelId)
                ? (await guild.GetVoiceChannelsAsync()).FirstOrDefault(x => x.Id == channelId)
                : null;
        }

        public static bool CheckViewChannelPermission(IGuildUser bot, IGuildChannel channel)
        {
            var permissions = bot.GetPermissions(channel);
            return permissions.ViewChannel;
        }
    }
}