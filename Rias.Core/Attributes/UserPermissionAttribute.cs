using System;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class UserPermissionAttribute : RiasCheckAttribute
    {
        public readonly GuildPermissions? GuildPermissions;

        public UserPermissionAttribute(Permission permissions)
        {
            GuildPermissions = permissions;
        }

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!GuildPermissions.HasValue)
                return CheckResult.Successful;

            var localization = context.ServiceProvider.GetRequiredService<Localization>();

            if (!(context.User is CachedMember member))
                return CheckResult.Unsuccessful(localization.GetText(null, Localization.AttributeUserPermissionNotGuild));

            if (member.Permissions.Has(GuildPermissions.Value))
                return CheckResult.Successful;

            var userPerms = member.Permissions.Permissions;
            var requiredPerms = GuildPermissions ^ (GuildPermissions & userPerms);

            var requiredPermsList = requiredPerms
                .GetValueOrDefault()
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);

            
            var guildId = context.Guild?.Id;
            var permsHumanized = requiredPermsList.Humanize(x => $"**{x.Titleize()}**",
                localization.GetText(guildId, Localization.CommonAnd).ToLowerInvariant());
            return CheckResult.Unsuccessful(localization.GetText(guildId, Localization.AttributeUserGuildPermissions, permsHumanized));
        }
    }
}