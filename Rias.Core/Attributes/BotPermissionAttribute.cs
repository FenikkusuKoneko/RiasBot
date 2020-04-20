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
    public class BotPermissionAttribute : RiasCheckAttribute
    {
        public readonly GuildPermissions? GuildPermissions;
        
        public BotPermissionAttribute(Permission permissions)
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

            if (currentMember.Permissions.Has(GuildPermissions.Value))
                return CheckResult.Successful;

            var botPerms = currentMember.Permissions.Permissions;
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