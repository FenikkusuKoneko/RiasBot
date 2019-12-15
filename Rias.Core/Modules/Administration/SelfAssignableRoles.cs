using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq.Extensions;
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
        [Name("Self Assignable Roles")]
        public class SelfAssignableRoles : RiasModule<SelfAssignableRolesService>
        {
            private readonly InteractiveService _interactive;

            public SelfAssignableRoles(IServiceProvider services) : base(services)
            {
                _interactive = services.GetRequiredService<InteractiveService>();
            }

            [Command("iam"), Context(ContextType.Guild),
             BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
            public async Task IamAsync([Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy((SocketGuildUser) Context.User) <= 0)
                {
                    await ReplyErrorAsync("YouAreAbove");
                    return;
                }

                var sar = Service.GetSelfAssignableRole(role);
                if (sar is null)
                {
                    await ReplyErrorAsync("RoleNotSelfAssignable", role.Name);
                    return;
                }

                var guildUser = (SocketGuildUser) Context.User;
                if (guildUser.Roles.Any(x => x.Id == sar.RoleId))
                {
                    await ReplyErrorAsync("YouAlreadyAre", role.Name);
                    return;
                }

                await guildUser.AddRoleAsync(role);
                await ReplyConfirmationAsync("YouAre", role.Name);
            }

            [Command("iamnot"), Context(ContextType.Guild),
             BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
            public async Task IamNotAsync([Remainder] SocketRole role)
            {
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy((SocketGuildUser) Context.User) <= 0)
                {
                    await ReplyErrorAsync("YouAreAbove");
                    return;
                }

                var sar = Service.GetSelfAssignableRole(role);
                if (sar is null)
                {
                    await ReplyErrorAsync("RoleNotSelfAssignable", role.Name);
                    return;
                }

                var guildUser = (SocketGuildUser) Context.User;
                if (guildUser.Roles.Any(x => x.Id == sar.RoleId))
                {
                    await guildUser.RemoveRoleAsync(role);
                }

                await ReplyConfirmationAsync("YouAreNot", role.Name);
            }

            [Command("addselfassignablerole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles)]
            public async Task AddSelfAssignableRoleAsync([Remainder] SocketRole role)
            {
                if (role.IsManaged)
                {
                    await ReplyErrorAsync("SarNotAdded", role.Name);
                    return;
                }

                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAbove");
                    return;
                }

                if (Service.GetSelfAssignableRole(role) is null)
                {
                    await Service.AddSelfAssignableRoleAsync(role);
                    await ReplyConfirmationAsync("SarAdded", role.Name);
                }
                else
                {
                    await ReplyErrorAsync("SarInList", role.Name);
                }
            }

            [Command("removeselfassignablerole"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles), BotPermission(GuildPermission.ManageRoles)]
            public async Task RemoveSelfAssignableRoleAsync([Remainder] SocketRole role)
            {
                var currentSar = Service.GetSelfAssignableRole(role);
                if (currentSar != null)
                {
                    await Service.RemoveSelfAssignableRoleAsync(currentSar);
                    await ReplyConfirmationAsync("SarRemoved", role.Name);
                }
                else
                {
                    await ReplyErrorAsync("SarNotInList", role.Name);
                }
            }

            [Command("listselfassignableroles"), Context(ContextType.Guild),
             BotPermission(GuildPermission.ManageRoles),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task ListSelfAssignableRolesAsync()
            {
                var sarList = await Service.UpdateSelfAssignableRolesAsync(Context.Guild!);
                if (sarList.Count == 0)
                {
                    await ReplyErrorAsync("NoSar");
                    return;
                }

                var index = 1;
                var pages = sarList.Values.Batch(15, x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = GetText("SarList"),
                        Color = RiasUtils.ConfirmColor,
                        Description = string.Join("\n", x.Select(sarDb => $"#{index++} {sarDb.RoleName} | {sarDb.RoleId}"))
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }
        }
    }
}