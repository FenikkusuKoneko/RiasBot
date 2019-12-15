using System;
using Discord.WebSocket;
using Rias.Core.Implementation;

namespace Rias.Core.Commons
{
    public class RoslynGlobals
    {
        public RiasCommandContext? Context { get; set; }
        public DiscordShardedClient? Client { get; set; }
        public DiscordSocketClient? SocketClient { get; set; }
        public IServiceProvider? Services { get; set; }
    }
}