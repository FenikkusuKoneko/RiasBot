using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration.Services
{
    public class AdministrationService : IKService
    {
        private readonly DiscordSocketClient _client;
        public AdministrationService(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task SoftBanPurge(IUser author, IGuild guild, IGuildUser user, IGuildUser bot, IMessageChannel channel)
        {
            var textChannels = await guild.GetTextChannelsAsync().ConfigureAwait(false);
            foreach (var c in textChannels)
            {
                var msgs = (await c.GetMessagesAsync(100).FlattenAsync()).Where((x) => x.Author.Id == user.Id);
                await c.DeleteMessagesAsync(msgs).ConfigureAwait(false);
            }
            await user.KickAsync().ConfigureAwait(false);
        }

        public async Task MuteService(IRole role, ICommandContext context)
        {
            OverwritePermissions permissions = new OverwritePermissions()
                .Modify(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Deny,
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit,
                PermValue.Inherit, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Inherit,
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit);

            var channels = await context.Guild.GetChannelsAsync();
            foreach (var c in channels)
            {
                await c.AddPermissionOverwriteAsync(role, permissions).ConfigureAwait(false);
            }
        }

        public bool CheckHierarchyRole(IGuild guild, IGuildUser user, IGuildUser bot)
        {
            var userRoles = new List<IRole>();
            var botRoles = new List<IRole>();

            foreach (var userRole in user.RoleIds)
                userRoles.Add(guild.GetRole(userRole));
            foreach (var botRole in bot.RoleIds)
                botRoles.Add(guild.GetRole(botRole));

            return botRoles.Any(x => userRoles.All(y => x.Position > y.Position));
        }
    }
}
