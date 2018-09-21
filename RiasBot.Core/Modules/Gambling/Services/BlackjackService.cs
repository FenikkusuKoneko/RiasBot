using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RiasBot.Modules.Gambling.Commons;
using RiasBot.Services;

namespace RiasBot.Modules.Gambling.Services
{
    public class BlackjackService : IRService
    {
        public readonly DiscordShardedClient Client;
        public readonly DbService Db;
        
        public BlackjackService(DiscordShardedClient client, DbService db)
        {
            Client = client;
            Db = db;
            client.ReactionAdded += ReactionsAdded;
            client.ReactionRemoved += ReactionsRemoved;
        }
        private readonly ConcurrentDictionary<ulong, BlackjackGame> _blackjackGames = new ConcurrentDictionary<ulong, BlackjackGame>();

        public BlackjackGame GetOrCreateGame(IGuildUser user)
        {
            var bj =  _blackjackGames.GetOrAdd(user.Id, new BlackjackGame(this));
            return bj;
        }
        
        public BlackjackGame GetGame(IGuildUser user)
        {
            _blackjackGames.TryGetValue(user.Id, out var bj);
            return bj;
        }

        public BlackjackGame RemoveGame(IGuildUser user)
        {
            _blackjackGames.TryRemove(user.Id, out var bj);
            return bj;
        }
        
        private async Task ReactionsAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var bj = GetGame((IGuildUser)reaction.User.Value);
            if (bj != null)
            {
                await bj.UpdateGameAsync(reaction).ConfigureAwait(false);
            }
        }
        
        private async Task ReactionsRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var bj = GetGame((IGuildUser)reaction.User.Value);
            if (bj != null)
            {
                if (!bj.ManageMessagesPermission)
                {
                    await bj.UpdateGameAsync(reaction).ConfigureAwait(false);
                }
            }
        }

        public int GetCurrency(IGuildUser user)
        {
            using (var db = Db.GetDbContext())
            {
                var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
                return userDb?.Currency ?? 0;
            }
        }
    }
}