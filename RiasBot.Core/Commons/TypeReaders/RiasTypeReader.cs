using Discord.Commands;
using Discord.WebSocket;

namespace RiasBot.Commons.TypeReaders
{
    public abstract class RiasTypeReader<T> : TypeReader
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandService _cmds;

        private RiasTypeReader() { }
        protected RiasTypeReader(DiscordShardedClient client, CommandService cmds)
        {
            _client = client;
            _cmds = cmds;
        }
    }
}