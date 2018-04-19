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

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class RolesCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly DbService _db;

            public RolesCommands(CommandHandler ch, CommandService service, DbService db)
            {
                _ch = ch;
                _service = service;
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Roles(int page = 1)
            {
                try
                {
                    var everyoneRole = Context.Guild.Roles.Where(x => x.Name == "@everyone");
                    var roles = Context.Guild.Roles.OrderByDescending(x => x.Position).Except(everyoneRole).Select(y => y.Name).ToArray();
                    await Context.Channel.SendPaginated((DiscordSocketClient)Context.Client, $"List of roles on this server", roles, 15, page - 1);
                }
                catch
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
                try
                {
                    var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    await role.DeleteAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(name)} was deleted successfully.").ConfigureAwait(false);

                }
                catch
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
                try
                {
                    var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();

                    var red = Convert.ToByte(Convert.ToInt32(color.Substring(0, 2), 16));
                    var green = Convert.ToByte(Convert.ToInt32(color.Substring(2, 2), 16));
                    var blue = Convert.ToByte(Convert.ToInt32(color.Substring(4, 2), 16));

                    await role.ModifyAsync(r => r.Color = new Color(red, green, blue)).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the color of role {Format.Bold(name)} was changed successfully.").ConfigureAwait(false);
                }
                catch
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
                try
                {
                    var roles = role.Split("->");
                    string oldName = roles[0].TrimEnd();
                    string newName = roles[1].TrimStart();

                    var oldRole = Context.Guild.Roles.Where(r => r.Name.ToLower() == oldName.ToLower()).First();
                    oldName = oldRole.Name;
                    await oldRole.ModifyAsync(r => r.Name = newName).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name of role {Format.Bold(oldName)} was renamed to {Format.Bold(oldRole.Name)} successfully.").ConfigureAwait(false);
                }
                catch
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
                try
                {
                    var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    await user.AddRoleAsync(role);
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} was added to {user.Mention} successfully.").ConfigureAwait(false);
                }
                catch
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
                try
                {
                    var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    await user.RemoveRoleAsync(role);

                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} was removed from {user.Mention} successfully.").ConfigureAwait(false);
                }
                catch
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
                            try
                            {
                                guildDb.AutoAssignableRole = getRole.Id;
                                await db.SaveChangesAsync().ConfigureAwait(false);
                            }
                            catch
                            {
                                var aar = new GuildConfig { GuildId = Context.Guild.Id, AutoAssignableRole = getRole.Id };
                                await db.AddAsync(aar).ConfigureAwait(false);
                                await db.SaveChangesAsync().ConfigureAwait(false);
                            }
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(getRole.Name)} will be auto-assigned to the new users.").ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        try
                        {
                            guildDb.AutoAssignableRole = 0;
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} auto-assignable role disabled.").ConfigureAwait(false);
                        }
                        catch
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
                try
                {
                    var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
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
                catch
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
                try
                {
                    var role = Context.Guild.Roles.Where(r => r.Name.ToLower() == name.ToLower()).FirstOrDefault();
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
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role couldn't be found.").ConfigureAwait(false);
                }
            }
        }
    }
}
