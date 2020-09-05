using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Administration
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
            public async Task RolesAsync(DiscordMember? member = null)
            {
                var roles = (member is null ? Context.Guild!.Roles.Select(x => x.Value) : ((DiscordMember) Context.User).Roles)
                    .Where(x => x.Id != Context.Guild!.EveryoneRole.Id)
                    .OrderByDescending(x => x.Position)
                    .Select(x => $"{x.Mention} | {x.Id}")
                    .ToList();

                if (roles.Count == 0)
                {
                    if (member is null)
                        await ReplyErrorAsync(Localization.AdministrationNoRoles);
                    else
                        await ReplyErrorAsync(Localization.AdministrationUserNoRoles, member.FullName());
                    return;
                }

                await SendPaginatedMessageAsync(roles, 15, (items, index) => new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = member is null ? GetText(Localization.AdministrationRolesList) : GetText(Localization.AdministrationUserRolesList, member.FullName()),
                    Description = string.Join("\n", items.Select(x => $"{++index}. {x}"))
                });
            }
            
            [Command("createrole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateRoleAsync([Remainder]string name)
            {
                await Context.Guild!.CreateRoleAsync(name);
                await ReplyConfirmationAsync(Localization.AdministrationRoleCreated, name);
            }
            
            [Command("deleterole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteRoleAsync([Remainder] DiscordRole role)
            {
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
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
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RoleColorAsync(DiscordRole role, DiscordColor color)
            {
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }
                
                await role.ModifyAsync(r => r.Color = color);
                await ReplyConfirmationAsync(Localization.AdministrationRoleColorChanged, role.Name);
            }
            
            [Command("renamerole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
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
                
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                oldName = role.Name;
                await role.ModifyAsync(r => r.Name = newName);
                await ReplyConfirmationAsync(Localization.AdministrationRoleRenamed, oldName, newName);
            }
            
            [Command("hoistrole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task HoistRoleAsync([Remainder] DiscordRole role)
            {
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (role.IsHoisted)
                {
                    await role.ModifyAsync(x => x.Hoist = false);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleNotDisplayed, role.Name);
                }
                else
                {
                    await role.ModifyAsync(x => x.Hoist = true);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleDisplayed, role.Name);
                }
            }
            
            [Command("mentionrole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task MentionRoleAsync([Remainder] DiscordRole role)
            {
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (role.IsMentionable)
                {
                    await role.ModifyAsync(x => x.Mentionable = false);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleNotMentionable, role.Name);
                }
                else
                {
                    await role.ModifyAsync(x => x.Mentionable = true);
                    await ReplyConfirmationAsync(Localization.AdministrationRoleMentionable, role.Name);
                }
            }
            
            [Command("addrole"), Context(ContextType.Guild),
             UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddRoleAsync(DiscordMember member, [Remainder] DiscordRole role)
            {
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (member.Roles.Any(x => x.Id == role.Id))
                {
                    await ReplyErrorAsync(Localization.AdministrationUserHasRole, member.FullName());
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotAdded, role.Name);
                    return;
                }

                await member.GrantRoleAsync(role);
                await ReplyConfirmationAsync(Localization.AdministrationRoleAdded, role.Name, member.FullName());
            }

            [Command("removerole"), Context(ContextType.Guild),
            UserPermission(Permissions.ManageRoles), BotPermission(Permissions.ManageRoles),
            Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RemoveRoleAsync(DiscordMember member, [Remainder] DiscordRole role)
            {
                if (role.Id == Context.Guild!.EveryoneRole.Id) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAbove);
                    return;
                }

                if (member.Roles.All(x => x.Id != role.Id))
                {
                    await ReplyErrorAsync(Localization.AdministrationUserNoRole, member.FullName());
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleNotRemoved);
                    return;
                }

                await member.RevokeRoleAsync(role);
                await ReplyConfirmationAsync(Localization.AdministrationRoleRemoved, role.Name, member.FullName());
            }

            [Command("autoassignablerole"), Context(ContextType.Guild),
             UserPermission(Permissions.Administrator),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AutoAssignableRoleAsync([Remainder] DiscordRole? role = null)
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
                
                if (((DiscordMember) Context.User).CheckRoleHierarchy(role) <= 0)
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
            
            private DiscordRole? GetRole(string value)
            {
                if (!RiasUtilities.TryParseRoleMention(value, out var roleId))
                    ulong.TryParse(value, out roleId);

                if (roleId > 0)
                    return Context.Guild!.GetRole(roleId);

                return Context.Guild!.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            }
        }
    }
}