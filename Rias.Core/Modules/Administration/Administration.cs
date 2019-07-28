using System.Linq;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Models;

namespace Rias.Core.Modules.Administration
{
    [Name("Administration")]
    public partial class Administration : RiasModule
    {
        [Command("setgreet"), Context(ContextType.Guild), UserPermission(GuildPermission.Administrator)]
        public async Task SetGreetAsync()
        {
            bool isGreetSet;
            var guildDb = Db.Guilds.FirstOrDefault(g => g.GuildId == Context.Guild.Id);

            if (guildDb != null)
            {
                if (!string.IsNullOrWhiteSpace(guildDb.GreetMessage))
                {
                    guildDb.Greet = isGreetSet = !guildDb.Greet;
                    guildDb.GreetChannel = Context.Channel.Id;
                    await Db.SaveChangesAsync();
                }
                else
                {
                    await ReplyErrorAsync("greet_message_not_set");
                    return;
                }
            }
            else
            {
                await ReplyErrorAsync("greet_message_not_set");
                return;
            }

            if (isGreetSet)
                await ReplyConfirmationAsync("greet_enabled", guildDb.GreetMessage);
            else
                await ReplyConfirmationAsync("greet_disabled");
        }

        [Command("greetmessage"), Context(ContextType.Guild), UserPermission(GuildPermission.Administrator)]
        public async Task GreetMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync("greet_message_length_limit", 1500);
                return;
            }

            var guildDb = Db.Guilds.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
            if (guildDb != null)
            {
                guildDb.GreetMessage = message;
            }
            else
            {
                var greetMsg = new Guilds {GuildId = Context.Guild.Id, GreetMessage = message};
                await Db.AddAsync(greetMsg);
            }

            await Db.SaveChangesAsync();
            await ReplyConfirmationAsync("greet_message_set");
        }

        [Command("setbye"), Context(ContextType.Guild), UserPermission(GuildPermission.Administrator)]
        public async Task SetByeAsync()
        {
            bool isByeSet;
            var guildDb = Db.Guilds.FirstOrDefault(g => g.GuildId == Context.Guild.Id);

            if (guildDb != null)
            {
                if (!string.IsNullOrWhiteSpace(guildDb.ByeMessage))
                {
                    guildDb.Bye = isByeSet = !guildDb.Bye;
                    guildDb.ByeChannel = Context.Channel.Id;
                    await Db.SaveChangesAsync();
                }
                else
                {
                    await ReplyErrorAsync("bye_message_not_set");
                    return;
                }
            }
            else
            {
                await ReplyErrorAsync("bye_message_not_set");
                return;
            }

            if (isByeSet)
                await ReplyConfirmationAsync("bye_enabled", guildDb.ByeMessage);
            else
                await ReplyConfirmationAsync("bye_disabled");
        }

        [Command("byemessage"), Context(ContextType.Guild), UserPermission(GuildPermission.Administrator)]
        public async Task ByeMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync("bye_message_length_limit", 1500);
                return;
            }

            var guildDb = Db.Guilds.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
            if (guildDb != null)
            {
                guildDb.ByeMessage = message;
            }
            else
            {
                var byeMsg = new Guilds {GuildId = Context.Guild.Id, ByeMessage = message};
                await Db.AddAsync(byeMsg);
            }

            await Db.SaveChangesAsync();
            await ReplyConfirmationAsync("bye_message_set");
        }

        [Command("setmodlog"), Context(ContextType.Guild), UserPermission(GuildPermission.Administrator)]
        public async Task SetModLogAsync()
        {
            var guildDb = Db.Guilds.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            if (guildDb != null)
            {
                if (guildDb.ModLogChannel != Context.Channel.Id)
                {
                    guildDb.ModLogChannel = Context.Channel.Id;
                    await ReplyConfirmationAsync("modlog_enabled");
                }
                else
                {
                    guildDb.ModLogChannel = 0;
                    await ReplyConfirmationAsync("modlog_disabled");
                }
            }
            else
            {
                var modlog = new Guilds { GuildId = Context.Guild.Id, ModLogChannel = Context.Channel.Id};
                await Db.AddAsync(modlog);
                await ReplyConfirmationAsync("modlog_enabled");
            }

            await Db.SaveChangesAsync();
        }
    }
}