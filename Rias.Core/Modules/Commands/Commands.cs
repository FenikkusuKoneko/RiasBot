using System;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Models;

namespace Rias.Core.Modules.Commands
{
    [Name("Commands")]
    public class Commands : RiasModule
    {
        public Commands(IServiceProvider services) : base(services) {}

        [Command("deletecommandmessage"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task DeleteCommandMessageAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new Guilds {GuildId = Context.Guild!.Id});
            guildDb.DeleteCommandMessage = !guildDb.DeleteCommandMessage;
            await DbContext.SaveChangesAsync();
            
            if (guildDb.DeleteCommandMessage)
                await ReplyConfirmationAsync("DeleteCommandMessageEnabled");
            else
                await ReplyConfirmationAsync("DeleteCommandMessageDisabled");
        }
    }
}