using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using RiasBot.Services;

namespace RiasBot.Modules.Administration.Services
{
    public class RolesService : IRService
    {
        public bool CheckHierarchyRole(IRole role, IGuild guild, IGuildUser bot)
        {
            var botRoles = new List<IRole>();

            foreach (var botRole in bot.RoleIds)
                botRoles.Add(guild.GetRole(botRole));

            return botRoles.Any(x => x.Position > role.Position);
        }
    }
}