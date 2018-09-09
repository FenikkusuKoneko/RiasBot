using Discord;
using Discord.Rest;
using System;

namespace RiasBot.Extensions
{
    public static class UserExtension
    {
        public static string GetRealAvatarUrl(this IGuildUser user, ushort size = 1024)
        {
            if (!string.IsNullOrEmpty(user.AvatarId))
            {
                return user.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{user.Id}/{user.AvatarId}.gif?size=1024"
                    : user.GetAvatarUrl(ImageFormat.Auto, size);
            }

            return GetDefaultAvatarUrl(user);
        }

        private static string GetDefaultAvatarUrl(this IGuildUser user)
            => $"{DiscordConfig.CDNUrl}embed/avatars/{user.DiscriminatorValue % 5}.png";

        public static string GetRealAvatarUrl(this IUser user, ushort size = 1024)
        {
            if (!string.IsNullOrEmpty(user.AvatarId))
            {
                return user.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{user.Id}/{user.AvatarId}.gif?size=1024"
                    : user.GetAvatarUrl(ImageFormat.Auto, size);
            }

            return GetDefaultAvatarUrl(user);
        }

        private static string GetDefaultAvatarUrl(this IUser user)
            => $"{DiscordConfig.CDNUrl}embed/avatars/{user.DiscriminatorValue % 5}.png";

        public static string GetRealAvatarUrl(this RestUser user, ushort size = 1024)
        {
            if (!string.IsNullOrEmpty(user.AvatarId))
            {
                return user.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{user.Id}/{user.AvatarId}.gif?size=1024"
                    : user.GetAvatarUrl(ImageFormat.Auto, size);
            }

            return GetDefaultAvatarUrl(user);
        }

        private static string GetDefaultAvatarUrl(this RestUser user)
            => $"{DiscordConfig.CDNUrl}embed/avatars/{user.DiscriminatorValue % 5}.png";
    }
}
