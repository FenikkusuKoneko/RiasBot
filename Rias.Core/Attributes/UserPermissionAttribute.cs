using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UserPermissionAttribute : RiasCheckAttribute
    {
        public readonly Permissions? GuildPermissions;

        public UserPermissionAttribute(Permissions permissions)
        {
            GuildPermissions = permissions;
        }

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!GuildPermissions.HasValue)
                return CheckResult.Successful;

            var localization = context.ServiceProvider.GetRequiredService<Localization>();

            if (!(context.User is DiscordMember member))
                return CheckResult.Unsuccessful(localization.GetText(null, Localization.AttributeUserPermissionNotGuild));

            if (member.GetPermissions().HasPermission(GuildPermissions.Value))
                return CheckResult.Successful;

            var userPerms = member.GetPermissions();
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