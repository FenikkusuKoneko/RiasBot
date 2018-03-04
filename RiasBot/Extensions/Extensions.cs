using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ImageSharp;
using RiasBot.Commons;
using SixLabors.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Extensions
{
    public static class Extensions
    {
        public static ModuleInfo GetModule(this ModuleInfo module)
        {
            if (module.Parent != null)
            {
                module = module.Parent;
            }
            return module;
        }

        public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client, Action<SocketReaction> reactionAdded, Action<SocketReaction> reactionRemoved = null)
        {
            if (reactionRemoved == null)
                reactionRemoved = delegate { };

            var wrap = new ReactionEventWrapper(client, msg);
            wrap.OnReactionAdded += (r) => { var _ = Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { var _ = Task.Run(() => reactionRemoved(r)); };
            return wrap;
        }
    }
}
