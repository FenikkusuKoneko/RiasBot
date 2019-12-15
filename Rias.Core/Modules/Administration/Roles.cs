using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Roles")]
        public class Roles : RiasModule<RolesService>
        {
            private readonly InteractiveService _interactive;

            public Roles(IServiceProvider services) : base(services)
            {
                _interactive = services.GetRequiredService<InteractiveService>();
            }

            [Command("roles"), Context(ContextType.Guild),
            Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RolesAsync()
            {
                var everyoneRole = Context.Guild!.Roles.Where(x => x.Id == Context.Guild!.EveryoneRole.Id);
                var roles = Context.Guild.Roles.OrderByDescending(x => x.Position)
                    .Except(everyoneRole)
                    .ToArray();

                if (roles.Length == 0)
                {
                    await ReplyErrorAsync("NoRoles");
                    return;
                }

                var index = 1;
                var pages = roles.Batch(15, x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = GetText("ListRoles"),
                        Color = RiasUtils.ConfirmColor,
                        Description = string.Join("\n", x.Select(role => $"#{index++} {role.Name} | {role.Id}"))
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }

            [Command("createrole"), Context(ContextType.Guild),
            UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
            Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateRoleAsync([Remainder]string name)
            {
                await Context.Guild!.CreateRoleAsync(name);
                await ReplyConfirmationAsync("RoleCreated", name);
            }

            [Command("deleterole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteRoleAsync([Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync("RoleNotDeleted", role.Name);
                    return;
                }

                await role.DeleteAsync();
                await ReplyConfirmationAsync("RoleDeleted", role.Name);
            }

            [Command("rolecolor"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RoleColorAsync(Color color, [Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }
                
                await role.ModifyAsync(r => r.Color = color);
                await ReplyConfirmationAsync("RoleColorChanged", role.Name);
            }

            [Command("renamerole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameRoleAsync([Remainder] string names)
            {
                var roles = names.Split("->", StringSplitOptions.RemoveEmptyEntries);
                var oldName = roles[0].TrimEnd();
                var newName = roles[1].TrimStart();

                var role = GetRole(oldName);
                if (role is null)
                {
                    await ReplyErrorAsync("RoleNotFound");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                oldName = role.Name;
                await role.ModifyAsync(r => r.Name = newName);
                await ReplyConfirmationAsync("RoleRenamed", oldName, newName);
            }

            [Command("hoistrole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task HoistRoleAsync([Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (role.IsHoisted)
                {
                    await role.ModifyAsync(x => x.Hoist = false);
                    await ReplyConfirmationAsync("RoleNotDisplayed", role.Name);
                }
                else
                {
                    await role.ModifyAsync(x => x.Hoist = true);
                    await ReplyConfirmationAsync("RoleDisplayed", role.Name);
                }
            }

            [Command("mentionrole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task MentionRoleAsync([Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (role.IsMentionable)
                {
                    await role.ModifyAsync(x => x.Mentionable = false);
                    await ReplyConfirmationAsync("RoleNotMentionable", role.Name);
                }
                else
                {
                    await role.ModifyAsync(x => x.Mentionable = true);
                    await ReplyConfirmationAsync("RoleMentionable", role.Name);
                }
            }

            [Command("addrole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddRoleAsync(SocketGuildUser user, [Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                if (user.Roles.Any(x => x.Id == role.Id))
                {
                    await ReplyErrorAsync("UserHasRole", user);
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync("RoleNotAdded", role.Name);
                    return;
                }

                await user.AddRoleAsync(role);
                await ReplyConfirmationAsync("RoleAdded", role.Name, user);
            }

            [Command("removerole"), Context(ContextType.Guild),
            UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles),
            Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RemoveRoleAsync(SocketGuildUser user, [Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                if (user.Roles.Any(x => x.Id != role.Id))
                {
                    await ReplyErrorAsync("UserNoRole", user);
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync("RoleNotRemoved");
                    return;
                }

                await user.RemoveRoleAsync(role);
                await ReplyConfirmationAsync("RoleRemoved", role.Name, user);
            }

            [Command("autoassignablerole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.Administrator),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AutoAssignableRoleAsync([Remainder] SocketRole? role = null)
            {
                if (role is null)
                {
                    await Service.RemoveAutoAssignableRoleAsync(Context.Guild!);
                    await ReplyConfirmationAsync("AarDisabled");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync("AarNotSet", role.Name);
                    return;
                }

                await Service.SetAutoAssignableRoleAsync(Context.Guild!, role);
                await ReplyConfirmationAsync("AarSet", role.Name);
            }

            private SocketRole GetRole(string value)
            {
                if (MentionUtils.TryParseRole(value, out var roleId))
                    return Context.Guild!.GetRole(roleId);

                if (ulong.TryParse(value, out var id))
                    return Context.Guild!.GetRole(id);

                return Context.Guild!.Roles.FirstOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}