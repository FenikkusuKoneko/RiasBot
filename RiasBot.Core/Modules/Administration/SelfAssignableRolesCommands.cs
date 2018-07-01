using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class SelfAssignableRolesCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly InteractiveService _is;
            private readonly DbService _db;
            public SelfAssignableRolesCommands(CommandHandler ch, CommandService service, InteractiveService interactiveService, DbService db)
            {
                _ch = ch;
                _service = service;
                _is = interactiveService;
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task Iam([Remainder]string name)
            {
                using (var db = _db.GetDbContext())
                {
                    var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    if (role != null)
                    {
                        if (db.SelfAssignableRoles.Where(x => x.GuildId == Context.Guild.Id).Any(y => y.RoleId == role.Id))
                        {
                            var user = (IGuildUser)Context.User;
                            await user.AddRoleAsync(role).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} you are now {Format.Bold(role.Name)}.");
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role {Format.Bold(role.Name)} is not self assignable.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the role.");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task Iamn([Remainder]string name)
            {
                using (var db = _db.GetDbContext())
                {
                    var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    if (role != null)
                    {
                        if (db.SelfAssignableRoles.Where(x => x.GuildId == Context.Guild.Id).Any(y => y.RoleId == role.Id))
                        {
                            var user = (IGuildUser)Context.User;
                            await user.RemoveRoleAsync(role).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} you are not {Format.Bold(role.Name)}.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the role.");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            public async Task AddSelfAssignableRole([Remainder]string name)
            {
                using (var db = _db.GetDbContext())
                {
                    var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    if (role != null)
                    {
                        if (!role.IsManaged)
                        {
                            if (!db.SelfAssignableRoles.Where(x => x.GuildId == Context.Guild.Id).Any(x => x.RoleId == role.Id))
                            {
                                var sar = new SelfAssignableRoles { GuildId = Context.Guild.Id, RoleName = role.Name, RoleId = role.Id };
                                await db.AddAsync(sar).ConfigureAwait(false);
                                await db.SaveChangesAsync().ConfigureAwait(false);

                                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} was added in the self assignable roles list successfully.").ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role {Format.Bold(role.Name)} is already in the self assignable roles list.").ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the role {Format.Bold(role.Name)} cannot be added to the self assignable roles list " +
                                $"because is automatically managed by Discord").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the role.").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteSelfAssignableRole([Remainder]string name)
            {
                using (var db = _db.GetDbContext())
                {
                    var role = Context.Guild.Roles.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();

                    if (role != null)
                    {
                        var sar = db.SelfAssignableRoles.Where(x => x.GuildId == Context.Guild.Id).Where(x => x.RoleId == role.Id).FirstOrDefault();
                        db.Remove(sar);
                        await db.SaveChangesAsync().ConfigureAwait(false);

                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(role.Name)} was deleted from the self assignable roles list successfully.");
                    }
                    else
                    {
                        var sar = db.SelfAssignableRoles.Where(x => x.GuildId == Context.Guild.Id).Where(x => x.RoleName.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
                        db.Remove(sar);
                        await db.SaveChangesAsync().ConfigureAwait(false);

                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} role {Format.Bold(name)} was deleted from the self assignable roles list successfully.");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ListSelfAssignableRoles()
            {
                using (var db = _db.GetDbContext())
                {
                    var sar = new List<SelfAssignableRoles>();
                    var checkSar = db.SelfAssignableRoles.Where(x => x.GuildId == Context.Guild.Id);
                    foreach (var validSar in checkSar)
                    {
                        var role = Context.Guild.GetRole(validSar.RoleId);
                        if (role != null)
                        {
                            sar.Add(validSar);
                        }
                        else
                        {
                            db.Remove(validSar);
                        }
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);

                    if (sar.Count > 0)
                    {
                        var lsar = new List<string>();
                        int index = 0;
                        foreach (var role in sar)
                        {
                            lsar.Add($"#{index + 1} {role.RoleName}");
                            index++;
                        }
                        var pager = new PaginatedMessage
                        {
                            Title = "Self assignable roles on this server",
                            Color = new Color(RiasBot.goodColor),
                            Pages = lsar,
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
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} no self assignable roles on this server.");
                    }
                }
            }
        }
    }
}
