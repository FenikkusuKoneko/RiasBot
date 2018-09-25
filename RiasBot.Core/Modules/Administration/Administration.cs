using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using RiasBot.Commons.Attributes;
using RiasBot.Services;
using RiasBot.Modules.Administration.Services;
using RiasBot.Services.Database.Models;
using RiasBot.Extensions;
using System;

namespace RiasBot.Modules.Administration
{
    public partial class Administration : RiasModule<AdministrationService>
    {
        private readonly DbService _db;

        public Administration( DbService db)
        {
            _db = db;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (user.Id == Context.User.Id)
                return;
            if (user.Id == Context.Guild.OwnerId)
                await Context.Channel.SendErrorMessageAsync("You cannot kick the owner of the server.").ConfigureAwait(false);
            else if (user.GuildPermissions.Administrator)
                await Context.Channel.SendErrorMessageAsync("You cannot kick an administrator.").ConfigureAwait(false);
            else
            {
                if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                {
                    await _service.KickUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, Context.Message, reason).ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("The user is above me in the hierarchy roles.").ConfigureAwait(false);
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(IGuildUser user, [Remainder] string reason = null)
        {
            if (user.Id == Context.User.Id)
                return;
            if (user.Id == Context.Guild.OwnerId)
                await Context.Channel.SendErrorMessageAsync("You cannot ban the owner of the server.").ConfigureAwait(false);
            else if (user.GuildPermissions.Administrator)
                await Context.Channel.SendErrorMessageAsync("You cannot ban an administrator.").ConfigureAwait(false);
            else
            {
                if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                {
                    await _service.BanUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, Context.Message, reason);
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("The user is above me in the hierarchy roles.").ConfigureAwait(false);
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers | GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.BanMembers | GuildPermission.ManageMessages)]
        public async Task SoftBanAsync(IGuildUser user, [Remainder]string reason = null)
        {
            if (user.Id == Context.User.Id)
                return;
            if (user.Id == Context.Guild.OwnerId)
                await Context.Channel.SendErrorMessageAsync("You cannot softban the owner of the server.").ConfigureAwait(false);
            else if (user.GuildPermissions.Administrator)
                await Context.Channel.SendErrorMessageAsync("You cannot softban an administrator.").ConfigureAwait(false);
            else
            {
                if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                {
                    await _service.SoftbanUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, Context.Message, reason);
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("The user is above me in the hierarchy roles.").ConfigureAwait(false);
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers | GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.BanMembers | GuildPermission.ManageMessages)]
        public async Task PruneBanAsync(IGuildUser user, [Remainder]string reason = null)
        {
            if (user.Id == Context.User.Id)
                return;
            if (user.Id == Context.Guild.OwnerId)
                await Context.Channel.SendErrorMessageAsync("You cannot pruneban the owner of the server.").ConfigureAwait(false);
            else if (user.GuildPermissions.Administrator)
                await Context.Channel.SendErrorMessageAsync("You cannot pruneban an administrator.").ConfigureAwait(false);
            else
            {
                if (_service.CheckHierarchyRole(Context.Guild, user, await Context.Guild.GetCurrentUserAsync()))
                {
                    await _service.PrunebanUser(Context.Guild, (IGuildUser)Context.User, user, Context.Channel, Context.Message, reason);
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("The user is above me in the hierarchy roles.").ConfigureAwait(false);
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task PruneAsync(int amount = 100)
        {
            var channel = (ITextChannel)Context.Channel;

            amount++;
            if (amount < 1)
                return;
            if (amount > 100)
                amount = 100;

            var msgs = (await channel.GetMessagesAsync(amount).FlattenAsync()).Where(m => DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14).ToList();
            if (msgs.Any())
            {
                await channel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendErrorMessageAsync("I couldn't delete any message because they are older than 14 days.");
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task PruneAsync(IGuildUser user, int amount = 100)
        {
            var channel = (ITextChannel)Context.Channel;
            if (user.Id == Context.Message.Author.Id)
                amount++;

            if (amount < 1)
                return;

            if (amount > 100)
                amount = 100;

            await Context.Message.DeleteAsync().ConfigureAwait(false);
            var msgs = (await channel.GetMessagesAsync().FlattenAsync()).Where(m => m.Author.Id == user.Id &&
                       DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                       .Take(amount).ToList();
            if (msgs.Any())
            {
                await channel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendErrorMessageAsync("I couldn't delete any message because they are older than 14 days.");
            }
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        public async Task PruneAsync(int amount, IGuildUser user)
        {
            var channel = (ITextChannel)Context.Channel;
            if (user.Id == Context.Message.Author.Id)
                amount++;

            if (amount < 1)
                return;

            if (amount > 100)
                amount = 100;

            await Context.Message.DeleteAsync().ConfigureAwait(false);
            var msgs = (await channel.GetMessagesAsync().FlattenAsync()).Where(m => m.Author.Id == user.Id &&
                       DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                       .Take(amount).ToList();
            if (msgs.Any())
            {
                await channel.DeleteMessagesAsync(msgs).ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendErrorMessageAsync("I couldn't delete any message because they are older than 14 days.");
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
                var greet = false;
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
                    await Context.Channel.SendConfirmationMessageAsync("Enabling announcements in this channel for users who join the server!");
                else
                    await Context.Channel.SendConfirmationMessageAsync("Disabling announcements for users who join the server!");
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
            await Context.Channel.SendConfirmationMessageAsync("New announcement message set for users who join the server!");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task Bye()
        {
            using (var db = _db.GetDbContext())
            {
                var bye = false;
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
                if (bye)
                    await Context.Channel.SendConfirmationMessageAsync("Enabling announcements in this channel for users who leave the server!");
                else
                    await Context.Channel.SendConfirmationMessageAsync("Disabling announcements for users who leave the server!");
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
            await Context.Channel.SendConfirmationMessageAsync("New announcement message set for users who leave the server!");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task ModLog()
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == Context.Guild.Id).FirstOrDefault();
                try
                {
                    if (guildDb.ModLogChannel != Context.Channel.Id)
                    {
                        guildDb.ModLogChannel = Context.Channel.Id;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} notifications about mute, unmute, kick, ban, unban will be posted in this channel.").ConfigureAwait(false);
                    }
                    else
                    {
                        guildDb.ModLogChannel = 0;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} notifications about mute, unmute, kick, ban, unban disabled.").ConfigureAwait(false);
                    }
                }
                catch
                {
                    var modlog = new GuildConfig { GuildId = Context.Guild.Id, ModLogChannel = Context.Channel.Id };
                    await db.AddAsync(modlog).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} notifications about mute, unmute, kick, ban, unban will be posted in this channel.").ConfigureAwait(false);
                }
            }
        }
    }
}
