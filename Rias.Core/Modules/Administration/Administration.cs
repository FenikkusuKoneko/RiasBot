using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Services;

namespace Rias.Core.Modules.Administration
{
    [Name("Administration")]
    public partial class Administration : RiasModule<AdministrationService>
    {
        public Administration(IServiceProvider services) : base(services) {}

        [Command("setgreet"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator), BotPermission(GuildPermission.ManageWebhooks)]
        public async Task SetGreetAsync()
        {
            var greetMsg = Service.GetGreetMessage(Context.Guild!);
            if (string.IsNullOrEmpty(greetMsg))
            {
                await ReplyErrorAsync("GreetMessageNotSet");
                return;
            }

            var greet = await Service.SetGreetAsync((SocketTextChannel) Context.Channel);
            if (greet)
                await ReplyConfirmationAsync("GreetEnabled", greetMsg);
            else
                await ReplyConfirmationAsync("GreetDisabled");
        }

        [Command("greetmessage"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task GreetMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync("GreetMessageLengthLimit", 1500);
                return;
            }

            await Service.SetGreetMessageAsync(Context.Guild!, message);
            await ReplyConfirmationAsync("GreetMessageSet");
        }

        [Command("setbye"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator), BotPermission(GuildPermission.ManageWebhooks)]
        public async Task SetByeAsync()
        {
            var byeMsg = Service.GetByeMessage(Context.Guild!);
            if (string.IsNullOrEmpty(byeMsg))
            {
                await ReplyErrorAsync("ByeMessageNotSet");
                return;
            }

            var bye = await Service.SetByeAsync((SocketTextChannel) Context.Channel);
            if (bye)
                await ReplyConfirmationAsync("ByeEnabled", byeMsg);
            else
                await ReplyConfirmationAsync("ByeDisabled");
        }

        [Command("byemessage"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task ByeMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync("ByeMessageLengthLimit", 1500);
                return;
            }

            await Service.SetByeMessageAsync(Context.Guild!, message);
            await ReplyConfirmationAsync("ByeMessageSet");
        }

        [Command("setmodlog"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task SetModLogAsync()
        {
            var modlog = await Service.SetModLogAsync(Context.Guild!, Context.Channel);
            if (modlog)
                await ReplyConfirmationAsync("ModLogEnabled");
            else
                await ReplyConfirmationAsync("ModLogDisabled");
        }
    }
}