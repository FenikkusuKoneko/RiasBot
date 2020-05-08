using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
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
             BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Member)]
            public async Task IamAsync([Remainder] CachedRole role)
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

                var member = (CachedMember) Context.User;
                if (member.GetRole(sarDb.RoleId) != null)
                {
                    await ReplyErrorAsync(Localization.AdministrationYouAlreadyAre, role.Name);
                    return;
                }

                await member.GrantRoleAsync(role.Id);
                await ReplyConfirmationAsync(Localization.AdministrationYouAre, role.Name);
            }
            
            [Command("iamnot"), Context(ContextType.Guild),
             BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Member)]
            public async Task IamNotAsync([Remainder] CachedRole role)
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

                var member = (CachedMember) Context.User;
                if (member.GetRole(sarDb.RoleId) != null)
                {
                    await member.RevokeRoleAsync(role.Id);
                }

                await ReplyConfirmationAsync(Localization.AdministrationYouAreNot, role.Name);
            }
            
            [Command("addselfassignablerole"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles)]
            public async Task AddSelfAssignableRoleAsync([Remainder] CachedRole role)
            {
                if (role.IsDefault) return;
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
                        GuildId = role.Guild.Id,
                        RoleId = role.Id,
                        RoleName = role.Name
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
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles)]
            public async Task RemoveSelfAssignableRoleAsync([Remainder] CachedRole role)
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
             BotPermission(Permission.ManageRoles),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task ListSelfAssignableRolesAsync()
            {
                var sarList = await UpdateSelfAssignableRolesAsync();
                if (sarList.Count == 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationNoSar);
                    return;
                }
                
                await SendPaginatedMessageAsync(sarList, 15, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationSarList),
                    Description = string.Join("\n", items.Select(x => $"{++index}. {x.RoleName} | {x.RoleId}"))
                });
            }
            
            private async Task<List<SelfAssignableRolesEntity>> UpdateSelfAssignableRolesAsync()
            {
                var sarList = await DbContext.GetListAsync<SelfAssignableRolesEntity>(x => x.GuildId == Context.Guild!.Id);
                foreach (var sar in sarList.ToArray())
                {
                    var role = Context.Guild!.GetRole(sar.RoleId);
                    if (role != null)
                    {
                        if (!string.Equals(sar.RoleName, role.Name))
                            sar.RoleName = role.Name;
                    }
                    else
                    {
                        sarList.Remove(sar);
                        DbContext.Remove(sar);
                    }
                }
                
                await DbContext.SaveChangesAsync();
                return sarList;
            }
        }
    }
}