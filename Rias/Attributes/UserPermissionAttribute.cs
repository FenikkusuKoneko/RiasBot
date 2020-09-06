using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UserPermissionAttribute : RiasCheckAttribute
    {
        private readonly Permissions? _guildPermissions;

        public UserPermissionAttribute(Permissions permissions)
        {
            _guildPermissions = permissions;
        }

        public Permissions? GuildPermissions => _guildPermissions;

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!_guildPermissions.HasValue)
                return CheckResult.Successful;

            var localization = context.ServiceProvider.GetRequiredService<Localization>();

            if (!(context.User is DiscordMember member))
                return CheckResult.Unsuccessful(localization.GetText(null, Localization.AttributeUserPermissionNotGuild));

            if (member.GetPermissions().HasPermission(_guildPermissions.Value))
                return CheckResult.Successful;

            var userPerms = member.GetPermissions();
            var requiredPerms = _guildPermissions ^ (_guildPermissions & userPerms);

            var requiredPermsList = requiredPerms
                .GetValueOrDefault()
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);
            
            var guildId = context.Guild?.Id;
            var permsHumanized = requiredPermsList.Humanize(x => $"**{x.Titleize()}**", localization.GetText(guildId, Localization.CommonAnd).ToLowerInvariant());
            return CheckResult.Unsuccessful(localization.GetText(guildId, Localization.AttributeUserGuildPermissions, permsHumanized));
        }
    }
}