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
        private readonly Permissions? _permissions;

        public UserPermissionAttribute(Permissions permissions)
        {
            _permissions = permissions;
        }

        public Permissions? Permissions => _permissions;

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!_permissions.HasValue)
                return CheckResult.Successful;

            var localization = context.ServiceProvider.GetRequiredService<Localization>();

            if (!(context.User is DiscordMember member))
                return CheckResult.Unsuccessful(localization.GetText(null, Localization.AttributeUserPermissionNotGuild));
            
            var guildPermissions = member.GetPermissions();
            var hasGuildPermissions = guildPermissions.HasPermission(_permissions.Value);
            
            var channelPermissions = member.PermissionsIn(context.Channel);
            var hasChannelPerm = channelPermissions.HasPermission(_permissions.Value);

            if (!hasGuildPermissions && !hasChannelPerm)
            {
                var guildPermsHumanized = HumanizePermissions(context.Guild!, guildPermissions, localization);
                return CheckResult.Unsuccessful(localization.GetText(context.Guild!.Id, Localization.AttributeUserGuildPermissions, guildPermsHumanized));
            }

            if (hasGuildPermissions && !hasChannelPerm)
            {
                var channelPermsHumanized = HumanizePermissions(context.Guild!, channelPermissions, localization);
                return CheckResult.Unsuccessful(localization.GetText(context.Guild!.Id, Localization.AttributeUserChannelPermissions, channelPermsHumanized));
            }

            return CheckResult.Successful;
        }

        private string HumanizePermissions(DiscordGuild guild, Permissions permissions, Localization localization)
        {
            var requiredPerms = _permissions ^ (_permissions & permissions);

            var requiredPermsList = requiredPerms
                .GetValueOrDefault()
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);
            
            return requiredPermsList.Humanize(x => $"**{x.Titleize()}**", localization.GetText(guild.Id, Localization.CommonAnd).ToLowerInvariant());
        }
    }
}