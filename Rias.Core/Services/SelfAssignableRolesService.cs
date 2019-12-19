using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class SelfAssignableRolesService : RiasService
    {
        public SelfAssignableRolesService(IServiceProvider services) : base(services)
        {
        }

        public SelfAssignableRoles? GetSelfAssignableRole(SocketRole role)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return db.SelfAssignableRoles.FirstOrDefault(x => x.GuildId == role.Guild.Id && x.RoleId == role.Id);
        }

        public async Task AddSelfAssignableRoleAsync(SocketRole role)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var sarDb = new SelfAssignableRoles
            {
                GuildId = role.Guild.Id,
                RoleId = role.Id,
                RoleName = role.Name
            };

            await db.AddAsync(sarDb);
            await db.SaveChangesAsync();
        }

        public async Task RemoveSelfAssignableRoleAsync(SelfAssignableRoles sarDb)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            db.Remove(sarDb);
            await db.SaveChangesAsync();
        }

        public async Task<IDictionary<ulong, SelfAssignableRoles>> UpdateSelfAssignableRolesAsync(SocketGuild guild)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var sarDict = db.SelfAssignableRoles.Where(x => x.GuildId == guild.Id).ToDictionary(x => x.RoleId);

            foreach (var (sarKey, sarValue) in sarDict)
            {
                var role = guild.GetRole(sarKey);
                if (role != null)
                {
                    if (!string.Equals(sarValue.RoleName, role.Name))
                        sarValue.RoleName = role.Name;
                }
                else
                {
                    sarDict.Remove(sarKey);
                    db.Remove(sarValue);
                }
            }

            await db.SaveChangesAsync();
            return sarDict;
        }
    }
}