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
    public class BotPermissionAttribute : RiasCheckAttribute
    {
        private readonly Permissions? _permissions;
        
        public BotPermissionAttribute(Permissions permissions)
        {
            _permissions = permissions;
        }
        
        public Permissions? Permissions => _permissions;

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!_permissions.HasValue)
                return CheckResult.Successful;

            var localization = context.Services.GetRequiredService<Localization>();

            var currentMember = context.CurrentMember;
            if (currentMember is null)
                return CheckResult.Failed(localization.GetText(null, Localization.AttributeBotPermissionNotGuild));

            var guildPermissions = currentMember.GetPermissions();
            var hasGuildPermissions = guildPermissions.HasPermission(_permissions.Value);
            
            var channelPermissions = currentMember.PermissionsIn(context.Channel);
            var hasChannelPerm = channelPermissions.HasPermission(_permissions.Value);

            if (!hasGuildPermissions && !hasChannelPerm)
            {
                var guildPermsHumanized = HumanizePermissions(context.Guild!, guildPermissions, localization);
                return CheckResult.Failed(localization.GetText(context.Guild!.Id, Localization.AttributeBotGuildPermissions, guildPermsHumanized));
            }

            if (hasGuildPermissions && !hasChannelPerm)
            {
                var channelPermsHumanized = HumanizePermissions(context.Guild!, channelPermissions, localization);
                return CheckResult.Failed(localization.GetText(context.Guild!.Id, Localization.AttributeBotChannelPermissions, channelPermsHumanized));
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

            return requiredPermsList.Humanize(x => $"**{x.Titleize()}**", localization!.GetText(guild.Id, Localization.CommonAnd).ToLowerInvariant());
        }
    }
}