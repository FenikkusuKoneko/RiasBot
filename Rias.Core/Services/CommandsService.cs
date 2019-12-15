using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class CommandsService : RiasService
    {
        public CommandsService(IServiceProvider services) : base(services) {}

        public async Task<bool> ToggleCommandMessageDeletionAsync(SocketGuild guild)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                guildDb.DeleteCommandMessage = !guildDb.DeleteCommandMessage;
                await db.SaveChangesAsync();
                return guildDb.DeleteCommandMessage;
            }

            var deleteCmdMsgDb = new Guilds { GuildId = guild.Id, DeleteCommandMessage = true };
            await db.AddAsync(deleteCmdMsgDb);

            await db.SaveChangesAsync();
            return true;
        }
    }
}