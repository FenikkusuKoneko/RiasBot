using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Rias.Core.Implementation
{
    public class RiasCommandContext : CommandContext
    {
        public DiscordSocketClient Client { get; }
        public SocketGuild Guild { get; }
        public IMessageChannel Channel { get; }
        public SocketUser User { get; }
        public SocketGuildUser CurrentGuildUser { get; }
        public SocketUserMessage Message { get; }

        public RiasCommandContext(DiscordShardedClient client, SocketUserMessage msg)
        {
            Guild = (msg.Channel as SocketGuildChannel)?.Guild;
            Client = client.GetShard(GetShardId(client, Guild));
            Channel = msg.Channel;
            User = msg.Author;
            CurrentGuildUser = Guild?.CurrentUser;
            Message = msg;
        }

        /// <summary> Gets the shard ID of the command context. </summary>
        private static int GetShardId(DiscordShardedClient client, IGuild guild)
        {
            return guild != null ? client.GetShardIdFor(guild) : 0;
        }
    }
}