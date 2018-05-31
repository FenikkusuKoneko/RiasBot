using Discord;
using Discord.Rest;

namespace RiasBot.Extensions
{
    public static class IUserExtension
    {
        public static string RealAvatarUrl(this IGuildUser user, ushort size = 128)
        {
            var url = user.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{user.Id}/{user.AvatarId}.gif?size=1024"
                    : user.GetAvatarUrl(ImageFormat.Auto, size);
            if (string.IsNullOrEmpty(url))
                return DefaultAvatarUrl(user);
            else
                return url;
        }

        public static string DefaultAvatarUrl(this IGuildUser user)
            => $"{DiscordConfig.CDNUrl}embed/avatars/{user.DiscriminatorValue % 5}.png";

        public static string RealAvatarUrl(this IUser user, ushort size = 128)
        {
            var url = user.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{user.Id}/{user.AvatarId}.gif?size=1024"
                    : user.GetAvatarUrl(ImageFormat.Auto, size);
            if (string.IsNullOrEmpty(url))
                return DefaultAvatarUrl(user);
            else
                return url;
        }

        public static string DefaultAvatarUrl(this IUser user)
            => $"{DiscordConfig.CDNUrl}embed/avatars/{user.DiscriminatorValue % 5}.png";

        public static string RealAvatarUrl(this RestUser user, ushort size = 128)
        {
            var url = user.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{user.Id}/{user.AvatarId}.gif?size=1024"
                    : user.GetAvatarUrl(ImageFormat.Auto, size);
            if (string.IsNullOrEmpty(url))
                return DefaultAvatarUrl(user);
            else
                return url;
        }

        public static string DefaultAvatarUrl(this RestUser user)
            => $"{DiscordConfig.CDNUrl}embed/avatars/{user.DiscriminatorValue % 5}.png";
    }
}
