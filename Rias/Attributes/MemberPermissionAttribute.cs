using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Commons;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MemberPermissionAttribute : RiasCheckAttribute
    {
        public MemberPermissionAttribute(Permissions permissions)
        {
            Permissions = permissions;
        }

        public Permissions Permissions { get; }

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            var localization = context.Services.GetRequiredService<Localization>();

            if (context.Guild is null)
            {
                return context.Command.Checks.Any(x => x is ContextAttribute contextAttribute && contextAttribute.Contexts.HasFlag(ContextType.Guild))
                    ? CheckResult.Successful
                    : CheckResult.Failed(localization.GetText(null, Localization.AttributeMemberPermissionNotGuild));
            }

            var member = (DiscordMember) context.User;
            var guildPermissions = member!.GetPermissions();
            var hasGuildPermissions = guildPermissions.HasPermission(Permissions);
            
            var channelPermissions = member.PermissionsIn(context.Channel);
            var hasChannelPerm = channelPermissions.HasPermission(Permissions);

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
            var requiredPerms = Permissions ^ (Permissions & permissions);

            var requiredPermsList = requiredPerms
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);
            
            return requiredPermsList.Humanize(x => $"**{x.Titleize()}**", localization.GetText(guild.Id, Localization.CommonAnd).ToLowerInvariant());
        }
    }
}