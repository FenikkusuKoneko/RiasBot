using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Database;

namespace Rias.Core.Modules.Utility
{
    [Name("Utility")]
    public class Utility : RiasModule
    {
        public IServiceProvider Services { get; set; }
        
        [Command("test")]
        public async Task TestAsync([Remainder]IGuildUser user)
        {
            await Context.Channel.SendMessageAsync(user.ToString());
        }
    }
}