using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Extensions
{
    public static class IUserExtension
    {
        public static string RealAvatarUrl(this IGuildUser user, ushort size = 128)
            => user.AvatarId.StartsWith("a_")
                    ? $"{DiscordConfig.CDNUrl}avatars/{user.Id}/{user.AvatarId}.gif"
                    : user.GetAvatarUrl(ImageFormat.Auto, size);

        public static string DefaultAvatarUrl(this IGuildUser user)
            => $"{DiscordConfig.CDNUrl}embed/avatars/{user.DiscriminatorValue % 5}.png";
    }
}
