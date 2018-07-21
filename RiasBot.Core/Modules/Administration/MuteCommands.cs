using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Core.Modules.Administration.Commons;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;

namespace RiasBot.Modules.Administration.Services
{
    public partial class Administration
    {
        public class MuteCommands : RiasSubmodule<MuteService>
        {
            private readonly DiscordShardedClient _client;
            private readonly AdministrationService _adminService;
            private readonly DbService _db;
            public MuteCommands(DiscordShardedClient client, AdministrationService adminService, DbService db)
            {
                _client = client;
                _adminService = adminService;
                _db = db;
            }
            
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageRoles | GuildPermission.MuteMembers)]
            [RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.MuteMembers)]
            [RequireContext(ContextType.Guild)]
            public async Task Mute(IGuildUser user, [Remainder]string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;
                if (user.Id != Context.Guild.OwnerId)
                {
                    if (_adminService.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                    {
                        await _service.MuteUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, TimeSpan.Zero, reason).ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the user is above the bot in the hierarchy roles.").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you cannot mute the owner of the server.").ConfigureAwait(false);
                }
            }
            
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageRoles | GuildPermission.MuteMembers)]
            [RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.MuteMembers)]
            [RequireContext(ContextType.Guild)]
            public async Task Mute(string time, IGuildUser user, [Remainder]string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;
                if (user.Id != Context.Guild.OwnerId)
                {
                    if (_adminService.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                    {
                        TimeSpan muteTime;
                        try
                        {
                            muteTime = MuteTime.GetMuteTime(time);
                        }
                        catch (ArgumentException ae)
                        {
                            await Context.Channel.SendErrorEmbed(ae.Message);
                            return;
                        }
                        await _service.MuteUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, muteTime, reason).ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the user is above the bot in the hierarchy roles.").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you cannot mute the owner of the server.").ConfigureAwait(false);
                }
            }
    
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task UnMute(IGuildUser user, [Remainder]string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;
                if (user.Id != Context.Guild.OwnerId)
                {
                    if (_adminService.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                    {
                        await _service.UnmuteUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, reason).ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the user is above the bot in the hierarchy roles.").ConfigureAwait(false);
                    }
                }
                else
                {
                    // doesn't matter
                } 
            }
            
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            [RequireContext(ContextType.Guild)]
            public async Task SetMute([Remainder]string name)
            {
                using (var db = _db.GetDbContext())
                {
                    var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
                    var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == name);
                    if (role is null)
                    {
                        role = await Context.Guild.CreateRoleAsync(name).ConfigureAwait(false);
                    }
                    if (guildDb != null)
                    {
                        guildDb.MuteRole = role.Id;
                    }
                    else
                    {
                        var muteRole = new GuildConfig { GuildId = Context.Guild.Id, MuteRole = role.Id };
                        await db.AddAsync(muteRole).ConfigureAwait(false);
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed("New mute role set.");
                    await Task.Factory.StartNew(() => _service.AddMuteRoleToChannels(role, Context.Guild));
                }
            }
        }
    }
}