using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class BotPermissionAttribute : RiasCheckAttribute
    {
        private readonly GuildPermission? _guildPermission;

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
        public BotPermissionAttribute(GuildPermission permission)
        {
            _guildPermission = permission;
        }

        protected override ValueTask<CheckResult> CheckAsync(RiasCommandContext context, IServiceProvider provider)
        {
            if (!_guildPermission.HasValue)
                return CheckResult.Successful;
            
            var tr = provider.GetRequiredService<Translations>();
            if (context.Guild is null)
                return CheckResult.Unsuccessful(tr.GetText(null, null, "#attribute_bot_perm_not_guild"));

            var botUser = context.Guild.CurrentUser;
            if (botUser.GuildPermissions.Has(_guildPermission.Value))
                return CheckResult.Successful;
            
            var botPerms = (GuildPermission) botUser.GuildPermissions.RawValue;
            var requiredPerms = _guildPermission ^ (_guildPermission & botPerms);
            
            var requiredPermsList = requiredPerms
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var guildId = context.Guild?.Id;
            var permString = tr.GetText(guildId, null, "#attribute_permission");
            if (requiredPermsList.Count > 1)
                permString = tr.GetText(guildId, null, "#attribute_permissions");

            return CheckResult.Unsuccessful(tr.GetText(guildId, null, "#attribute_bot_perm_guild", 
                requiredPermsList.Humanize(x => $"**{x.Titleize()}**", 
                    tr.GetText(guildId, null, "#common_and").ToLowerInvariant()), permString.ToLowerInvariant()));
        }
    }
}