using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Rias.Core.Extensions
{
    public static class UserExtensions
    {
        public static Permissions GetPermissions(this DiscordMember member)
        {
            if (member.IsOwner)
                return Permissions.All;
            
            var permissions = member.Roles.Aggregate(Permissions.None, (p, r) => p | r.Permissions);
            return (permissions & Permissions.Administrator) == Permissions.Administrator ? Permissions.All : permissions;
        }

        public static IReadOnlyDictionary<ulong, DiscordGuild> GetMutualGuilds(this DiscordMember member, RiasBot bot)
            => bot.Guilds.Where(x => x.Value.Members.ContainsKey(member.Id))
                .Select(x => x.Value)
                .ToImmutableDictionary(x => x.Id);

        /// <summary>
        /// Check the hierarchy between the current member and another member in the roles hierarchy
        /// </summary>
        /// <returns>A value lower than 0 if the current member is below the other member<br/>
        /// A value equal with 0 if both members are in the highest role<br/>
        /// A value greater than 0 if current member is above the other member<br/>
        /// The value returned is the difference between their highest role position</returns>
        public static int CheckHierarchy(this DiscordMember currentMember, DiscordMember member)
            => currentMember.Hierarchy - member.Hierarchy;

        /// <summary>
        /// Check the hierarchy between the current member and a role in the roles hierarchy
        /// </summary>
        /// <returns>A value lower than 0 if the current member's highest role is below the role<br/>
        /// A value equal with 0 if the current member's highest role is the role that is checked<br/>
        /// A value greater than 0 if current member's highest role is above the role<br/>
        /// The value returned is the difference between the member's highest role position and the role's position</returns>
        public static int CheckRoleHierarchy(this DiscordMember member, DiscordRole role)
            => member.Hierarchy - role.Position;

        public static string FullName(this DiscordUser user)
            => $"{user.Username}#{user.Discriminator}";
    }
}