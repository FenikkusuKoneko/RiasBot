using System;
using System.Threading.Tasks;
using DSharpPlus;
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
        private readonly Permissions? _guildPermissions;
        
        public Permissions? GuildPermissions => _guildPermissions;

        public BotPermissionAttribute(Permissions permissions)
        {
            _guildPermissions = permissions;
        }

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!_guildPermissions.HasValue)
                return CheckResult.Successful;

            var localization = context.ServiceProvider.GetRequiredService<Localization>();

            var currentMember = context.CurrentMember;
            if (currentMember is null)
                return CheckResult.Unsuccessful(localization.GetText(null, Localization.AttributeBotPermissionNotGuild));

            if (currentMember.GetPermissions().HasPermission(_guildPermissions.Value))
                return CheckResult.Successful;

            var botPerms = currentMember.GetPermissions();
            var requiredPerms = _guildPermissions ^ (_guildPermissions & botPerms);

            var requiredPermsList = requiredPerms
                .GetValueOrDefault()
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);

            var guildId = context.Guild?.Id;
            var permsHumanized = requiredPermsList.Humanize(
                x => $"**{x.Titleize()}**", localization.GetText(guildId, Localization.CommonAnd).ToLowerInvariant());
            return CheckResult.Unsuccessful(localization.GetText(guildId, Localization.AttributeBotGuildPermissions, permsHumanized));
        }
    }
}