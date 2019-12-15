using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Services.Commons;
using Serilog;

namespace Rias.Core.Services
{
    public class BlackjackService : RiasService
    {
        private readonly GamblingService _gamblingService;
        
        public BlackjackService(IServiceProvider services) : base(services)
        {
            _gamblingService = services.GetRequiredService<GamblingService>();
            _cards = InitializeCards();
            
            _sessions = new ConcurrentDictionary<ulong, BlackjackGame>();

            services.GetRequiredService<DiscordShardedClient>().ReactionAdded += ReactionAddedAsync;
        }

        private readonly IEnumerable<(int, string)> _cards;
        private readonly string[] _suits = {"♠", "♥", "♣", "♦"};
        private readonly string[] _highCards = {"A", "J", "Q", "K"};
        
        private readonly ConcurrentDictionary<ulong, BlackjackGame> _sessions;
        private readonly string _moduleName = "Gambling";
        
        public readonly Emoji CardEmoji = new Emoji("🎴");
        public readonly Emoji HandEmoji = new Emoji("🤚");
        public readonly Emoji SplitEmoji = new Emoji("↔");
        
        public string GetText(ulong guildId, string key, params object[] args)
            => base.GetText(guildId, _moduleName, key, args);

        private IEnumerable<(int, string)> InitializeCards()
        {
            for (var i = 0; i < 14; i++)
            {
                var cardNumber = i + 1;
                switch (i)
                {
                    case 0:
                    case 11:
                    case 12:
                    case 13:
                        yield return (cardNumber, _highCards[i % 10]);
                        continue;
                    case 10:
                        continue;
                    default:
                        yield return (cardNumber, cardNumber.ToString());
                        break;
                }
            }
        }

        public bool TryGetBlackjack(SocketGuildUser user, out BlackjackGame? blackjack)
            => _sessions.TryGetValue(user.Id, out blackjack);

        public async Task CreateBlackjackAsync(SocketGuildUser user, IMessageChannel channel, int bet)
        {
            var blackjack = new BlackjackGame(this, user, bet, _suits, _cards);
            await blackjack.CreateAsync(channel);

            await blackjack.Message!.AddReactionAsync(CardEmoji);
            await blackjack.Message!.AddReactionAsync(HandEmoji);
            if (blackjack.PlayerCanSplit)
                await blackjack.Message!.AddReactionAsync(SplitEmoji);

            await TakeUserCurrencyAsync(user, bet);
            _sessions[user.Id] = blackjack;
            
            Log.Debug("Blackjack: Session created");
        }

        public bool TryRemoveBlackjack(SocketGuildUser user, out BlackjackGame? blackjack)
            => _sessions.TryRemove(user.Id, out blackjack);

        public int GetUserCurrency(SocketUser user)
            => _gamblingService.GetUserCurrency(user);

        public Task AddUserCurrencyAsync(SocketUser user, int currency)
            => _gamblingService.AddUserCurrencyAsync(user.Id, currency);
        
        public Task TakeUserCurrencyAsync(SocketUser user, int currency)
            => _gamblingService.RemoveUserCurrencyAsync(user, currency);

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified)
                return;
            if (!(reaction.User.Value is SocketGuildUser guildUser))
                return;
            if (!_sessions.TryGetValue(guildUser.Id, out var blackjack))
                return;
            if (blackjack.Message!.Id != message.Id)
                return;

            if (reaction.Emote.Equals(CardEmoji))
            {
                _ = RunTaskAsync(blackjack.HitAsync());
                await blackjack.Message!.RemoveReactionAsync(CardEmoji, guildUser);
            }

            if (reaction.Emote.Equals(HandEmoji))
            {
                _ = RunTaskAsync(blackjack.StandAsync());
                await blackjack.Message!.RemoveReactionAsync(HandEmoji, guildUser);
            }
            
            if (reaction.Emote.Equals(SplitEmoji) && blackjack.PlayerCanSplit)
            {
                _ = RunTaskAsync(blackjack.SplitAsync());
                await blackjack.Message!.RemoveReactionAsync(SplitEmoji, guildUser);
            }
        }
    }
}