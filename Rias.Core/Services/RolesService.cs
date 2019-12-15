using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class RolesService : RiasService
    {
        public RolesService(IServiceProvider services) : base(services) {}

        public async Task SetAutoAssignableRoleAsync(SocketGuild guild, SocketRole role)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);

            if (guildDb != null)
            {
                guildDb.AutoAssignableRoleId = role.Id;
            }
            else
            {
                var aar = new Guilds
                {
                    GuildId = guild.Id,
                    AutoAssignableRoleId = role.Id
                };
                await db.AddAsync(aar);
            }

            await db.SaveChangesAsync();
        }

        public async Task RemoveAutoAssignableRoleAsync(SocketGuild guild)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                guildDb.AutoAssignableRoleId = 0;
                await db.SaveChangesAsync();
            }
        }
    }
}