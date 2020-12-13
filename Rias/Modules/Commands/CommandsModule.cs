using System;
using System.Threading.Tasks;
using DSharpPlus;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Implementation;

namespace Rias.Modules.Commands
{
    [Name("Commands")]
    public class CommandsModule : RiasModule
    {
        public CommandsModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        
        [Command("deletecommandmessage", "delcmdmsg")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task DeleteCommandMessageAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity { GuildId = Context.Guild!.Id });
            guildDb.DeleteCommandMessage = !guildDb.DeleteCommandMessage;
            await DbContext.SaveChangesAsync();
            
            if (guildDb.DeleteCommandMessage)
                await ReplyConfirmationAsync(Localization.CommandsDeleteCommandMessageEnabled);
            else
                await ReplyConfirmationAsync(Localization.CommandsDeleteCommandMessageDisabled);
        }
    }
}