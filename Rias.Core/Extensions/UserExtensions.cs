using System.Linq;
using System.Threading.Tasks;
using Disqord;

namespace Rias.Core.Extensions
{
    public static class UserExtensions
    {
        /// <summary>
        /// Check the hierarchy between the current member and another member in the roles hierarchy
        /// </summary>
        /// <returns>A value lower than 0 if the current member is below the other member<br/>
        /// A value equal with 0 if both members are in the highest role<br/>
        /// A value greater than 0 if current member is above the other member<br/>
        /// The value returned is the difference between their highest role position</returns>
        public static int CheckHierarchy(this CachedMember currentMember, CachedMember member)
        {
            if (member.Id != member.Guild.OwnerId)
                return currentMember.Hierarchy - member.Hierarchy;

            var memberHighestRole = member.Roles
                .OrderByDescending(x => x.Value.Position)
                .First().Value;
                
            var currentMemberHighestRole = currentMember.Roles
                .OrderByDescending(x => x.Value.Position)
                .First().Value;

            return currentMemberHighestRole.Position - memberHighestRole.Position;
        }

        /// <summary>
        /// Check the hierarchy between the current member and a role in the roles hierarchy
        /// </summary>
        /// <returns>A value lower than 0 if the current member's highest role is below the role<br/>
        /// A value equal with 0 if the current member's highest role is the role that is checked<br/>
        /// A value greater than 0 if current member's highest role is above the role<br/>
        /// The value returned is the difference between the member's highest role position and the role's position</returns>
        public static int CheckRoleHierarchy(this CachedMember member, IRole role)
            => member.Hierarchy - role.Position;

        public static async Task<IUserMessage> SendMessageAsync(this CachedMember user, LocalEmbedBuilder embed)
            => await user.SendMessageAsync(embed: embed.Build());
    }
}