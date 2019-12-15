using System;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Rias.Core.Implementation
{
    public class RiasCommandContext : CommandContext
    {
        /// <summary>
        /// Gets the current client.
        /// </summary>
        public DiscordSocketClient Client { get; }

        /// <summary>
        /// Gets the guild where the command was executed.
        /// </summary>
        public SocketGuild? Guild { get; }

        /// <summary>
        /// Gets the channel where the command was executed.
        /// </summary>
        public IMessageChannel Channel { get; }

        /// <summary>
        /// Gets the user that executed the command.
        /// </summary>
        public SocketUser User { get; }

        /// <summary>
        /// Gets the current logged-in user.
        /// </summary>
        public SocketGuildUser? CurrentGuildUser { get; }

        /// <summary>
        /// Gets the user's message that executed the command.
        /// </summary>
        public SocketUserMessage Message { get; }

        public RiasCommandContext(DiscordShardedClient client, SocketUserMessage message, IServiceProvider services) : base(services)
        {
            Guild = (message.Channel as SocketGuildChannel)?.Guild;
            Client = client.GetShard(GetShardId(client, Guild));
            Channel = message.Channel;
            User = message.Author;
            CurrentGuildUser = Guild?.CurrentUser;
            Message = message;
        }

        /// <summary> Gets the shard ID of the command context. </summary>
        private static int GetShardId(DiscordShardedClient client, SocketGuild? guild)
        {
            return guild != null ? client.GetShardIdFor(guild) : 0;
        }
    }
}