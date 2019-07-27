using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class UserPermissionAttribute : RiasCheckAttribute
    {
        public readonly GuildPermission? GuildPermission;

        /// <summary>
        ///     Requires that the user invoking the command to have a specific <see cref="Discord.GuildPermission"/>.
        /// </summary>
        /// <remarks>
        ///     This precondition will always fail if the command is being invoked in a <see cref="IPrivateChannel"/>.
        /// </remarks>
        /// <param name="permission">
        ///     The <see cref="Discord.GuildPermission" /> that the user must have. Multiple permissions can be
        ///     specified by ORing the permissions together.
        /// </param>
        public UserPermissionAttribute(GuildPermission permission)
        {
            GuildPermission = permission;
        }

        protected override ValueTask<CheckResult> CheckAsync(RiasCommandContext context, IServiceProvider provider)
        {
            if (!GuildPermission.HasValue)
                return CheckResult.Successful;
            
            var tr = provider.GetRequiredService<Translations>();

            if (!(context.User is SocketGuildUser guildUser))
                return CheckResult.Unsuccessful(tr.GetText(null, null, "#attribute_user_perm_not_guild"));

            if (guildUser.GuildPermissions.Has(GuildPermission.Value))
                return CheckResult.Successful;
            
            var userPerms = (GuildPermission) guildUser.GuildPermissions.RawValue;
            var requiredPerms = GuildPermission ^ (GuildPermission & userPerms);
            
            var requiredPermsList = requiredPerms
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);

            var guildId = context.Guild?.Id;
            var permString = tr.GetText(guildId, null, "#attribute_permission");
            if (requiredPermsList.Length > 1)
                permString = tr.GetText(guildId, null, "#attribute_permissions");

            return CheckResult.Unsuccessful(tr.GetText(guildId, null, "#attribute_user_perm_guild",
                requiredPermsList.Humanize(x => $"**{x.Titleize()}**",
                    tr.GetText(guildId, null, "#common_and").ToLowerInvariant()), permString.ToLowerInvariant()));
        }
    }
}