using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;

namespace Rias.Core.Modules.Utility
{
    [Name("Utility")]
    public class Utility : RiasModule
    {
        public IServiceProvider Services { get; set; }
        
        [Command("test")]
        public async Task TestAsync(SocketGuildUser user)
        {
            new PerformanceCounter
            await Context.Channel.SendMessageAsync(user.ToString());
        }
    }
}