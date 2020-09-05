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
        public readonly Permissions? GuildPermissions;
        
        public BotPermissionAttribute(Permissions permissions)
        {
            GuildPermissions = permissions;
        }

        public override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!GuildPermissions.HasValue)
                return CheckResult.Successful;

            var localization = context.ServiceProvider.GetRequiredService<Localization>();

            var currentMember = context.CurrentMember;
            if (currentMember is null)
                return CheckResult.Unsuccessful(localization.GetText(null, Localization.AttributeBotPermissionNotGuild));

            if (currentMember.GetPermissions().HasPermission(GuildPermissions.Value))
                return CheckResult.Successful;

            var botPerms = currentMember.GetPermissions();
            var requiredPerms = GuildPermissions ^ (GuildPermissions & botPerms);

            var requiredPermsList = requiredPerms
                .GetValueOrDefault()
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);

            var guildId = context.Guild?.Id;
            var permsHumanized = requiredPermsList.Humanize(x => $"**{x.Titleize()}**",
                localization.GetText(guildId, Localization.CommonAnd).ToLowerInvariant());
            return CheckResult.Unsuccessful(localization.GetText(guildId, Localization.AttributeBotGuildPermissions, permsHumanized));
        }
    }
}