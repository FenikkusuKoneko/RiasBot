using Discord.WebSocket;

namespace Rias.Core.Extensions
{
    public static class UserExtensions
    {
        public static string GetRealAvatarUrl(this SocketUser user)
        {
            return user.GetAvatarUrl(size: 2048) ?? user.GetDefaultAvatarUrl();
        }
    }
}