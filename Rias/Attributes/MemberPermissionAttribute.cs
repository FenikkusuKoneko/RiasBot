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
    public class MemberPermissionAttribute : RiasCheckAttribute
    {
        private readonly Permissions? _permissions;

        public MemberPermissionAttribute(Permissions permissions)
        {
            _permissions = permissions;
        }

        public Permissions? Permissions => _permissions;

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!_permissions.HasValue)
                return CheckResult.Successful;

            var localization = context.Services.GetRequiredService<Localization>();
            
            if (context.User is not DiscordMember member)
                return CheckResult.Failed(localization.GetText(null, Localization.AttributeMemberPermissionNotGuild));

            var guildPermissions = member!.GetPermissions();
            var hasGuildPermissions = guildPermissions.HasPermission(_permissions.Value);
            
            var channelPermissions = member.PermissionsIn(context.Channel);
            var hasChannelPerm = channelPermissions.HasPermission(_permissions.Value);

            if (!hasGuildPermissions && !hasChannelPerm)
            {
                var guildPermsHumanized = HumanizePermissions(context.Guild!, guildPermissions, localization);
                return CheckResult.Failed(localization.GetText(context.Guild!.Id, Localization.AttributeMemberGuildPermissions, guildPermsHumanized));
            }

            if (hasGuildPermissions && !hasChannelPerm)
            {
                var channelPermsHumanized = HumanizePermissions(context.Guild!, channelPermissions, localization);
                return CheckResult.Failed(localization.GetText(context.Guild!.Id, Localization.AttributeMemberChannelPermissions, channelPermsHumanized));
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