using System;
using Discord;
using Discord.WebSocket;

namespace Rias.Core.Extensions
{
    public static class UserExtensions
    {
        public static string GetRealAvatarUrl(this SocketUser user)
        {
            return user.GetAvatarUrl(size: 2048) ?? user.GetDefaultAvatarUrl();
        }

        /// <summary>
        /// Check the hierarchy between the current user and another user in the roles hierarchy
        /// </summary>
        /// <returns>A value lower than 0 if the current user is below the other user<br/>
        /// A value equal with 0 if both users are in the highest role<br/>
        /// A value greater than 0 if current user is above the other user<br/>
        /// The value returned is the difference between their highest role position</returns>
        /// <exception cref="InvalidCastException">It is thrown if the <see cref="IGuildUser"/> is not <see cref="SocketGuildUser"/></exception>
        public static int CheckHierarchy(this IGuildUser userOne, IGuildUser userTwo)
        {
            if (!(userOne is SocketGuildUser socketGuildUserOne))
                throw new InvalidCastException("The current IGuildUser user is not SocketGuildUser.");

            if (!(userTwo is SocketGuildUser socketGuildUserTwo))
                throw new InvalidCastException("The IGuildUser user to check is not SocketGuildUser.");

            return socketGuildUserOne.Hierarchy - socketGuildUserTwo.Hierarchy;
        }

        /// <summary>
        /// Check the hierarchy between the current user and a role in the roles hierarchy
        /// </summary>
        /// <returns>A value lower than 0 if the current user's highest role is below the role<br/>
        /// A value equal with 0 if the current user's highest role is the role that is checked<br/>
        /// A value greater than 0 if current user's highest role is above the role<br/>
        /// The value returned is the difference between the user's highest role position and the role's position</returns>
        /// <exception cref="InvalidCastException">It is thrown if the <see cref="IGuildUser"/> is not <see cref="SocketGuildUser"/></exception>
        public static int CheckRoleHierarchy(this IGuildUser user, IRole role)
        {
            if (!(user is SocketGuildUser socketGuildUser))
                throw new InvalidCastException("The IGuildUser user is not SocketGuildUser.");

            return socketGuildUser.Hierarchy - role.Position;
        }
    }
}