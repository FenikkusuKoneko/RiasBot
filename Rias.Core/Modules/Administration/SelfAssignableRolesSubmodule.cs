using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Self Assignable Roles")]
        public class SelfAssignableRolesSubmodule : RiasModule
        {
            public SelfAssignableRolesSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("iam"), Context(ContextType.Guild),
             BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Member)]
            public async Task IamAsync([Remainder] DiscordRole role)
            {
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }

                var sarDb = await DbContext.SelfAssignableRoles.FirstOrDefaultAsync(x => x.GuildId == Context.Guild!.Id && x.RoleId == role.Id);
                if (sarDb is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotSelfAssignable, role.Name);
                    return;
                }

                var member = (DiscordMember) Context.User;
                if (member.Roles.Any(x => x.Id == sarDb.RoleId))
                {
                    await ReplyErrorAsync(Localization.AdministrationYouAlreadyAre, role.Name);
                    return;
                }

                await member.GrantRoleAsync(role);
                await ReplyConfirmationAsync(Localization.AdministrationYouAre, role.Name);
            }
            
            [Command("iamnot"), Context(ContextType.Guild),
             BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Member)]
            public async Task IamNotAsync([Remainder] DiscordRole role)
            {
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }

                var sarDb = await DbContext.SelfAssignableRoles.FirstOrDefaultAsync(x => x.GuildId == Context.Guild!.Id && x.RoleId == role.Id);
                if (sarDb is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotSelfAssignable, role.Name);
                    return;
                }

                var member = (DiscordMember) Context.User;
                if (member.Roles.Any(x => x.Id == sarDb.RoleId))
                {
                    await member.RevokeRoleAsync(role);
                }

                await ReplyConfirmationAsync(Localization.AdministrationYouAreNot, role.Name);
            }
            
            [Command("addselfassignablerole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles)]
            public async Task AddSelfAssignableRoleAsync([Remainder] DiscordRole role)
            {
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationSarNotAdded, role.Name);
                    return;
                }

                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }

                var sarDb = await DbContext.SelfAssignableRoles.FirstOrDefaultAsync(x => x.GuildId == Context.Guild!.Id && x.RoleId == role.Id);
                if (sarDb is null)
                {
                    await DbContext.AddAsync(new SelfAssignableRolesEntity
                    {
                        GuildId = Context.Guild.Id,
                        RoleId = role.Id
                    });

                    await DbContext.SaveChangesAsync();
                    await ReplyConfirmationAsync(Localization.AdministrationSarAdded, role.Name);
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationSarInList, role.Name);
                }
            }

            [Command("removeselfassignablerole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles)]
            public async Task RemoveSelfAssignableRoleAsync([Remainder] DiscordRole role)
            {
                var sarDb = await DbContext.SelfAssignableRoles.FirstOrDefaultAsync(x => x.GuildId == Context.Guild!.Id && x.RoleId == role.Id);
                if (sarDb != null)
                {
                    DbContext.Remove(sarDb);
                    await DbContext.SaveChangesAsync();
                    await ReplyConfirmationAsync(Localization.AdministrationSarRemoved, role.Name);
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationSarNotInList, role.Name);
                }
            }
            
            [Command("listselfassignableroles"), Context(ContextType.Guild),
             BotPermission(Permissions.ManageRoles),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task ListSelfAssignableRolesAsync()
            {
                var roles = await UpdateSelfAssignableRolesAsync();
                if (roles.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationNoSar);
                    return;
                }
                
                await SendPaginatedMessageAsync(roles, 15, (items, index) => new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationSarList),
                    Description = string.Join("\n", items.Select(x => $"{++index}. {x.Mention} | {x.Id}"))
                });
            }
            
            private async Task<List<DiscordRole>> UpdateSelfAssignableRolesAsync()
            {
                var roles = new List<DiscordRole>();
                var sarList = await DbContext.GetListAsync<SelfAssignableRolesEntity>(x => x.GuildId == Context.Guild!.Id);
                
                foreach (var sar in sarList)
                {
                    var role = Context.Guild!.GetRole(sar.RoleId);
                    if (role != null)
                        roles.Add(role);
                    else
                        DbContext.Remove(sar);
                }
                
                await DbContext.SaveChangesAsync();
                return roles;
            }
        }
    }
}