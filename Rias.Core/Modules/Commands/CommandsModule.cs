using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Commands
{
    [Name("Commands")]
    public class CommandsModule : RiasModule
    {
        public CommandsModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        [Command("deletecommandmessage"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task DeleteCommandMessageAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            guildDb.DeleteCommandMessage = !guildDb.DeleteCommandMessage;
            await DbContext.SaveChangesAsync();
            
            if (guildDb.DeleteCommandMessage)
                await ReplyConfirmationAsync(Localization.CommandsDeleteCommandMessageEnabled);
            else
                await ReplyConfirmationAsync(Localization.CommandsDeleteCommandMessageDisabled);
        }
    }
}