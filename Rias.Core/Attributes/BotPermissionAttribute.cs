using System;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Implementation;

namespace Rias.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class BotPermissionAttribute : RiasCheckAttribute
    {
        public readonly GuildPermission? GuildPermission;

        /// <summary>
        ///     Requires that the user invoking the command to have a specific <see cref="Discord.GuildPermission"/>.
        /// </summary>
        /// <remarks>
        ///     This precondition will always fail if the command is being invoked in a <see cref="IPrivateChannel"/>.
        /// </remarks>
        /// <param name="permission">
        ///     The <see cref="Discord.GuildPermission" /> that the user must have. Multiple permissions can be
        ///     specified by ORing the permissions together.
        /// </param>
        public BotPermissionAttribute(GuildPermission permission)
        {
            GuildPermission = permission;
        }

        protected override ValueTask<CheckResult> CheckAsync(RiasCommandContext context)
        {
            if (!GuildPermission.HasValue)
                return CheckResult.Successful;

            var resources = context.ServiceProvider.GetRequiredService<Resources>();
            if (context.Guild is null)
                return CheckResult.Unsuccessful(resources.GetText(null, "Attribute", "BotPermissionNotGuild"));

            var botUser = context.CurrentGuildUser;
            if (botUser is null)
                return CheckResult.Unsuccessful(resources.GetText(null, "Attribute", "BotPermissionNotGuild"));

            if (botUser.GuildPermissions.Has(GuildPermission.Value))
                return CheckResult.Successful;

            var botPerms = (GuildPermission) botUser.GuildPermissions.RawValue;
            var requiredPerms = GuildPermission ^ (GuildPermission & botPerms);

            var requiredPermsList = requiredPerms
                .GetValueOrDefault()
                .ToString()
                .Split(",", StringSplitOptions.RemoveEmptyEntries);

            var guildId = context.Guild?.Id;
            var permsHumanized = requiredPermsList.Humanize(x => $"**{x.Titleize()}**", resources.GetText(guildId, "Common", "And").ToLowerInvariant());
            
            var permStringKey = requiredPermsList.Length > 1 ? "BotGuildPermissions" : "BotGuildPermission";
            return CheckResult.Unsuccessful(resources.GetText(guildId, "Attribute", permStringKey, permsHumanized));
        }
    }
}