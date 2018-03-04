using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using RiasBot.Commons.Attributes;
using RiasBot.Services;
using System.Collections.Generic;
using RiasBot.Modules.Administration.Services;
using RiasBot.Services.Database.Models;
using RiasBot.Extensions;

namespace RiasBot.Modules.Administration
{
    public partial class Administration : RiasModule<AdministrationService>
    {
        public readonly CommandHandler _ch;
        public readonly DbService _db;

        public Administration(CommandHandler ch, CommandService service, DbService db)
        {
            _ch = ch;
            _db = db;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, [Remainder] string reason = null)
        {
            if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
            {
                var kickEmbed = new EmbedBuilder();
                kickEmbed.WithColor(RiasBot.color);
                kickEmbed.WithDescription("User kicked");
                kickEmbed.AddField("Username", $"{user}").AddField("ID", user.Id.ToString(), true);
                if (reason != null)
                    kickEmbed.AddField("Reason", reason);

                await ReplyAsync("", embed: kickEmbed.Build()).ConfigureAwait(false);

                var reasonEmbed = new EmbedBuilder();
                reasonEmbed.WithColor(RiasBot.color);
                reasonEmbed.WithDescription($"You have been kicked from {Format.Bold(Context.Guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);

                await user.KickAsync(reason).ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the user is above the bot in the hierarchy roles.").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task Ban(IGuildUser user, [Remainder] string reason = null)
        {
            if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
            {
                var banEmbed = new EmbedBuilder();
                banEmbed.WithColor(RiasBot.color);
                banEmbed.WithDescription("User banned");
                banEmbed.AddField("Username", $"{user}").AddField("ID", user.Id.ToString(), true);
                if (reason != null)
                    banEmbed.AddField("Reason", reason);

                await ReplyAsync("", embed: banEmbed.Build()).ConfigureAwait(false);

                var reasonEmbed = new EmbedBuilder();
                reasonEmbed.WithColor(RiasBot.color);
                reasonEmbed.WithDescription($"You have been banned from {Format.Bold(Context.Guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);


                await Context.Guild.AddBanAsync(user).ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the user is above the bot in the hierarchy roles.").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers | GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.KickMembers | GuildPermission.ManageMessages)]
        public async Task SoftBan(IGuildUser user, [Remainder]string reason = null)
        {
            if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
            {
                var banEmbed = new EmbedBuilder();
                banEmbed.WithColor(RiasBot.color);
                banEmbed.WithDescription("User soft-banned");
                banEmbed.AddField("Username", $"{user}").AddField("ID", user.Id.ToString(), true);
                if (reason != null)
                    banEmbed.AddField("Reason", reason);

                await ReplyAsync("", embed: banEmbed.Build()).ConfigureAwait(false);

                var reasonEmbed = new EmbedBuilder();
                reasonEmbed.WithColor(RiasBot.color);
                reasonEmbed.WithDescription($"You have been kicked from {Format.Bold(Context.Guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                await Task.Factory.StartNew(async () => _service.SoftBanPurge(Context.User, Context.Guild, user, await Context.Guild.GetCurrentUserAsync(), Context.Channel));
            }
            else
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the user is above the bot in the hierarchy roles.").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task Mute([Remainder]IGuildUser user)
        {
            if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
            {
                using (var db = _db.GetDbContext())
                {
                    var guildDb = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                    try
                    {
                        IRole role = null;
                        try
                        {
                            role = Context.Guild.GetRole(guildDb.MuteRole);
                        }
                        catch
                        {
                            role = await Context.Guild.CreateRoleAsync("kurumi-mute").ConfigureAwait(false);
                            var newRole = new GuildConfig { GuildId = Context.Guild.Id, MuteRole = role.Id };
                            await db.AddAsync(newRole).ConfigureAwait(false);
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                        if (user.RoleIds.Any(r => r == role.Id))
                        {
                            await Context.Channel.SendConfirmationEmbed($"{user.Mention} is already muted from text and voice channels!");
                        }
                        else
                        {
                            await Task.Factory.StartNew(() => _service.MuteService(role, Context));
                            await user.AddRoleAsync(role).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{user.Mention} has been muted from text and voice thannels!");
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the user is above the bot in the hierarchy roles.").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task UnMute([Remainder]IGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                try
                {
                    IRole role = null;
                    try
                    {
                        role = Context.Guild.GetRole(guildDb.MuteRole);
                    }
                    catch
                    {
                        role = await Context.Guild.CreateRoleAsync("kurumi-mute").ConfigureAwait(false);
                        var newRole = new GuildConfig { GuildId = Context.Guild.Id, MuteRole = role.Id };
                        await db.AddAsync(newRole).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    if (user.RoleIds.Any(r => r == role.Id))
                    {
                        await user.RemoveRoleAsync(role).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{user.Mention} has been unmuted from text and voice thannels!");
                    }
                    else
                    {
                        await Context.Channel.SendConfirmationEmbed($"{user.Mention} is already muted from text and voice channels!");
                    }
                }
                catch
                {

                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task Purge(int amount)
        {
            var channel = (ITextChannel)Context.Channel;

            amount++;

            if (amount < 1)
                return;

            if (amount > 100)
                amount = 100;

            var msgs = await channel.GetMessagesAsync(amount).FlattenAsync();
            await Task.Delay(1000).ConfigureAwait(false);
            await channel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [Priority(0)]
        public async Task Purge(IGuildUser user, int amount = 100)
        {
            var channel = (ITextChannel)Context.Channel;
            if (user.Id == Context.Message.Author.Id)
                amount++;

            if (amount < 1)
                return;

            if (amount > 100)
                amount = 100;

            var msgs = (await channel.GetMessagesAsync(100).FlattenAsync()).Where((x) => x.Author.Id == user.Id).Take(amount).ToArray();
            await Task.Delay(1000).ConfigureAwait(false);
            await channel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
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
                var guildDb = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                try
                {
                    var role = Context.Guild.Roles.Where(x => x.Name == name).FirstOrDefault();
                    if (role is null)
                    {
                        role = await Context.Guild.CreateRoleAsync(name).ConfigureAwait(false);
                    }
                    guildDb.MuteRole = role.Id;

                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed("New mute role setted.");
                }
                catch
                {

                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task Greet()
        {
            using (var db = _db.GetDbContext())
            {
                bool greet = false;
                var guildDb = db.Guilds.Where(g => g.GuildId == Context.Guild.Id).FirstOrDefault();
                try
                {
                    greet = !guildDb.Greet;
                    guildDb.Greet = greet;
                    guildDb.GreetChannel = Context.Channel.Id;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                catch
                {
                    var Greet = new GuildConfig { GuildId = Context.Guild.Id, Greet = true, GreetChannel = Context.Channel.Id };
                    await db.AddAsync(Greet).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    greet = true;
                }
                if (greet)
                    await Context.Channel.SendConfirmationEmbed("Enabling announcements in this channel for users who join the server!");
                else
                    await Context.Channel.SendConfirmationEmbed("Disabling announcements for users who join the server!");
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task GreetMessage([Remainder]string message)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(g => g.GuildId == Context.Guild.Id).FirstOrDefault();
                try
                {
                    guildDb.GreetMessage = message;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                catch
                {
                    var greetMsg = new GuildConfig { GuildId = Context.Guild.Id, GreetMessage = message };
                    await db.AddAsync(greetMsg).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            await Context.Channel.SendConfirmationEmbed("New announcement message setted for users who join the server!");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task Bye()
        {
            using (var db = _db.GetDbContext())
            {
                bool bye = false;
                var guildDb = db.Guilds.Where(g => g.GuildId == Context.Guild.Id).FirstOrDefault();
                try
                {
                    bye = !guildDb.Bye;
                    guildDb.Bye = bye;
                    guildDb.ByeChannel = Context.Channel.Id;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    
                }
                catch
                {
                    var Bye = new GuildConfig { GuildId = Context.Guild.Id, Bye = true, ByeChannel = Context.Channel.Id };
                    await db.AddAsync(Bye).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    bye = true;
                }
                if (guildDb.Bye)
                    await Context.Channel.SendConfirmationEmbed("Enabling announcements in this channel for users who leave the server!");
                else
                    await Context.Channel.SendConfirmationEmbed("Disabling announcements for users who leave the server!");
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task ByeMessage([Remainder]string message)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(g => g.GuildId == Context.Guild.Id).FirstOrDefault();
                try
                {
                    guildDb.ByeMessage = message;
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                catch
                {
                    var byeMsg = new GuildConfig { GuildId = Context.Guild.Id, ByeMessage = message };
                    await db.AddAsync(byeMsg).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            await Context.Channel.SendConfirmationEmbed("New announcement message setted for users who leave the server!");
        }
    }
}
