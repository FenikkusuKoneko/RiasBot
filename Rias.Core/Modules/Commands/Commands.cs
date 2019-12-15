using System;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Services;

namespace Rias.Core.Modules.Commands
{
    [Name("Commands")]
    public class Commands : RiasModule<CommandsService>
    {
        public Commands(IServiceProvider services) : base(services) {}

        [Command("deletecommandmessage"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task DeleteCommandMessageAsync()
        {
            if (await Service.ToggleCommandMessageDeletionAsync(Context.Guild!))
                await ReplyConfirmationAsync("DeleteCommandMessageEnabled");
            else
                await ReplyConfirmationAsync("DeleteCommandMessageDisabled");
        }
    }
}