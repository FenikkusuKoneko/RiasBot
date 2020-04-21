using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
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
        [Name("Roles")]
        public class RolesSubmodule : RiasModule
        {
            public RolesSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("roles"), Context(ContextType.Guild),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Member)]
            public async Task RolesAsync(CachedMember? member = null)
            {
                var roles = (member is null ? Context.Guild!.Roles : ((CachedMember) Context.User).Roles)
                    .Where(x => x.Key != Context.Guild!.DefaultRole.Id)
                    .OrderByDescending(x => x.Value.Position)
                    .Select(x => $"{x.Value.Mention} | {x.Value.Id}")
                    .ToList();

                if (roles.Count == 0)
                {
                    if (member is null)
                        await ReplyErrorAsync(Localization.AdministrationNoRoles);
                    else
                        await ReplyErrorAsync(Localization.AdministrationUserNoRoles, member);
                    return;
                }

                await SendPaginatedMessageAsync(roles, 15, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = member is null ? GetText(Localization.AdministrationRolesList) : GetText(Localization.AdministrationUserRolesList, member),
                    Description = string.Join("\n", items.Select(x => $"{++index}. {x}"))
                });
            }
            
            [Command("createrole"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateRoleAsync([Remainder]string name)
            {
                await Context.Guild!.CreateRoleAsync(x => x.Name = name);
                await ReplyConfirmationAsync(Localization.AdministrationRoleCreated, name);
            }
            
            [Command("deleterole"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteRoleAsync([Remainder] CachedRole role)
            {
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotDeleted, role.Name);
                    return;
                }

                await role.DeleteAsync();
                await ReplyConfirmationAsync(Localization.AdministrationRoleDeleted, role.Name);
            }
            
            [Command("rolecolor"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RoleColorAsync(CachedRole role, Color color)
            {
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }
                
                await role.ModifyAsync(r => r.Color = color);
                await ReplyConfirmationAsync(Localization.AdministrationRoleColorChanged, role.Name);
            }
            
            [Command("renamerole"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameRoleAsync([Remainder] string names)
            {
                var roles = names.Split("->", StringSplitOptions.RemoveEmptyEntries);
                var oldName = roles[0].TrimEnd();
                var newName = roles[1].TrimStart();

                var role = GetRole(oldName);
                if (role is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotFound);
                    return;
                }
                
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                oldName = role.Name;
                await role.ModifyAsync(r => r.Name = newName);
                await ReplyConfirmationAsync(Localization.AdministrationRoleRenamed, oldName, newName);
            }
            
            [Command("hoistrole"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task HoistRoleAsync([Remainder] CachedRole role)
            {
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (role.IsHoisted)
                {
                    await role.ModifyAsync(x => x.IsHoisted = false);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleNotDisplayed, role.Name);
                }
                else
                {
                    await role.ModifyAsync(x => x.IsHoisted = true);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleDisplayed, role.Name);
                }
            }
            
            [Command("mentionrole"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task MentionRoleAsync([Remainder] CachedRole role)
            {
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (role.IsMentionable)
                {
                    await role.ModifyAsync(x => x.IsMentionable = false);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleNotMentionable, role.Name);
                }
                else
                {
                    await role.ModifyAsync(x => x.IsMentionable = true);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleMentionable, role.Name);
                }
            }
            
            [Command("addrole"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddRoleAsync(CachedMember member, [Remainder] CachedRole role)
            {
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (member.GetRole(role.Id) != null)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserHasRole, member);
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotAdded, role.Name);
                    return;
                }

                await member.GrantRoleAsync(role.Id);
                await ReplyConfirmationAsync(Localization.AdministrationRoleAdded, role.Name, member);
            }

            [Command("removerole"), Context(ContextType.Guild),
            UserPermission(Permission.ManageRoles), BotPermission(Permission.ManageRoles),
            Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RemoveRoleAsync(CachedMember member, [Remainder] CachedRole role)
            {
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (member.GetRole(role.Id) is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNoRole, member);
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotRemoved);
                    return;
                }

                await member.RevokeRoleAsync(role.Id);
                await ReplyConfirmationAsync(Localization.AdministrationRoleRemoved, role.Name, member);
            }

            [Command("autoassignablerole"), Context(ContextType.Guild),
             UserPermission(Permission.Administrator),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AutoAssignableRoleAsync([Remainder] CachedRole? role = null)
            {
                GuildsEntity guildDb;
                if (role is null)
                {
                    guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                    guildDb.AutoAssignableRoleId = 0;
                    await DbContext.SaveChangesAsync();
                    await ReplyConfirmationAsync(Localization.AdministrationAarDisabled);
                    return;
                }

                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationAarNotSet, role.Name);
                    return;
                }
                
                guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                guildDb.AutoAssignableRoleId = role.Id;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.AdministrationAarSet, role.Name);
            }
            
            private CachedRole GetRole(string value)
            {
                if (Discord.TryParseRoleMention(value, out var roleId))
                    return Context.Guild!.GetRole(roleId);

                if (Snowflake.TryParse(value, out var id))
                    return Context.Guild!.GetRole(id);

                return Context.Guild!.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            }
        }
    }
}