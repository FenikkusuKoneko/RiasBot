using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using System.Globalization;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class RolesCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly InteractiveService _is;
            private readonly DbService _db;

            public RolesCommands(CommandHandler ch, CommandService service, InteractiveService interactiveService, DbService db)
            {
                _ch = ch;
                _service = service;
                _is = interactiveService;
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Roles()
            {
                var everyoneRole = Context.Guild.Roles.Where(x => x.Name == "@everyone");
                var roles = Context.Guild.Roles.OrderByDescending(x => x.Position).Except(everyoneRole).Select(y => y.Name).ToList();
                if (roles.Count > 0)
                {
                    var pager = new PaginatedMessage
                    {
                        Title = "List of roles on this server",
                        Color = new Color(RiasBot.GoodColor),
                        Pages = roles,
                        Options = new PaginatedAppearanceOptions
                        {
                            ItemsPerPage = 15,
                            Timeout = TimeSpan.FromMinutes(1),
                            DisplayInformationIcon = false,
                            JumpDisplayOptions = JumpDisplayOptions.Never
                        }

                    };
                    await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} No roles on this server.");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task CreateRole([Remainder]string name)
            {
                await Context.Guild.CreateRoleAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(name)} was created successfully.").ConfigureAwait(false);
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task DeleteRole([Remainder]string name)
            {
                var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (role != null)
                {
                    if (!role.IsManaged)
                    {
                        await role.DeleteAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(name)} was deleted successfully.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role {Format.Bold(role.Name)} cannot be deleted " +
                                $"because is automatically managed by Discord").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be deleted.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RoleColor(string color, [Remainder]string name)
            {
                color = color.Replace("#", "");
                if (color.Length != 6)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the color is not a valid hex color.").ConfigureAwait(false);
                    return;
                }
                var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (role != null)
                {
                    if (Int32.TryParse(color.Substring(0, 2), NumberStyles.HexNumber, null, out var redColor) &&
                        Int32.TryParse(color.Substring(2, 2), NumberStyles.HexNumber, null, out var greenColor) &&
                        Int32.TryParse(color.Substring(4, 2), NumberStyles.HexNumber, null, out var blueColor))
                    {
                        var red = Convert.ToByte(redColor);
                        var green = Convert.ToByte(greenColor);
                        var blue = Convert.ToByte(blueColor);
                        await role.ModifyAsync(r => r.Color = new Color(red, green, blue)).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the color of role {Format.Bold(name)} was changed successfully.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the color is not a valid hex color.").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RenameRole([Remainder]string role)
            {
                var roles = role.Split("->");
                var oldName = roles[0].TrimEnd();
                var newName = roles[1].TrimStart();

                var oldRole = Context.Guild.Roles.Where(r => r.Name.ToLower() == oldName.ToLower()).First();
                if (oldRole != null)
                {
                    oldName = oldRole.Name;
                    await oldRole.ModifyAsync(r => r.Name = newName).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name of role {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully.").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task SetRole(IGuildUser user, [Remainder] string name)
            {
                var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (role != null)
                {
                    if (!role.IsManaged)
                    {
                        await user.AddRoleAsync(role);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} was added to {user.Mention} successfully.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role {Format.Bold(role.Name)} cannot be added " +
                                $"because is automatically managed by Discord").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role or the user couldn't be found.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task RemoveRole(IGuildUser user, [Remainder] string name)
            {
                var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (role != null)
                {
                    await user.RemoveRoleAsync(role).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} was removed from {user.Mention} successfully.").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role or the user couldn't be found.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task AutoAssignableRole([Remainder]string role = null)
            {
                using (var db = _db.GetDbContext())
                {
                    var guildDb = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();

                    if (!String.IsNullOrEmpty(role))
                    {
                        var getRole = Context.Guild.Roles.Where(x => x.Name.ToLower() == role.ToLower()).FirstOrDefault();
                        if (getRole != null)
                        {
                            if (!getRole.IsManaged)
                            {
                                if (guildDb != null)
                                {
                                    guildDb.AutoAssignableRole = getRole.Id;
                                    await db.SaveChangesAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    var aar = new GuildConfig { GuildId = Context.Guild.Id, AutoAssignableRole = getRole.Id };
                                    await db.AddAsync(aar).ConfigureAwait(false);
                                    await db.SaveChangesAsync().ConfigureAwait(false);
                                }
                                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(getRole.Name)} will be auto-assigned to the new users.").ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role {Format.Bold(getRole.Name)} cannot be auto-assigned " +
                                $"because is automatically managed by Discord").ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (guildDb != null)
                        {
                            guildDb.AutoAssignableRole = 0;
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} auto-assignable role disabled.").ConfigureAwait(false);
                        }
                        else
                        {
                            var aar = new GuildConfig { GuildId = Context.Guild.Id, AutoAssignableRole = 0 };
                            await db.AddAsync(aar).ConfigureAwait(false);
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task HoistRole([Remainder]string name)
            {
                var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (role != null)
                {
                    if (role.IsHoisted)
                    {
                        await role.ModifyAsync(x => x.Hoist = false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} is not displayed independently in the userlist.").ConfigureAwait(false);
                    }
                    else
                    {
                        await role.ModifyAsync(x => x.Hoist = true);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} is now displayed independently in the userlist.").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task MentionRole([Remainder]string name)
            {
                var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                if (role != null)
                {
                    if (role.IsMentionable)
                    {
                        await role.ModifyAsync(x => x.Mentionable = false).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} is not mentionable.").ConfigureAwait(false);
                    }
                    else
                    {
                        await role.ModifyAsync(x => x.Mentionable = true).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} is now mentionable.").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                }
            }
        }
    }
}
