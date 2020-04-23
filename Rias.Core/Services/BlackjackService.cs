using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Services.Commons;
using Serilog;

namespace Rias.Core.Services
{
    public class BlackjackService : RiasService
    {
        private readonly GamblingService _gamblingService;
        
        public BlackjackService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _gamblingService = serviceProvider.GetRequiredService<GamblingService>();
            _cards = InitializeCards();
            RiasBot.ReactionAdded += ReactionAddedAsync;
            RiasBot.ReactionRemoved += ReactionRemovedAsync;
        }

        private readonly IEnumerable<(int, string)> _cards;
        private readonly string[] _suits = {"♠", "♥", "♣", "♦"};
        private readonly string[] _highCards = {"A", "J", "Q", "K"};
        
        private readonly ConcurrentDictionary<Snowflake, BlackjackGame> _sessions = new ConcurrentDictionary<Snowflake, BlackjackGame>();
        
        public readonly IEmoji CardEmoji = new LocalEmoji("🎴");
        public readonly IEmoji HandEmoji = new LocalEmoji("🤚");
        public readonly IEmoji SplitEmoji = new LocalEmoji("↔");
        
        public bool TryGetBlackjack(Snowflake userId, out BlackjackGame? blackjack)
            => _sessions.TryGetValue(userId, out blackjack);
        
        public async Task CreateBlackjackAsync(CachedMember member, CachedTextChannel channel, int bet)
        {
            var blackjack = new BlackjackGame(this, member, bet, _suits, _cards);
            await blackjack.CreateAsync(channel);

            await blackjack.Message!.AddReactionAsync(CardEmoji);
            await blackjack.Message!.AddReactionAsync(HandEmoji);
            if (blackjack.PlayerCanSplit)
                await blackjack.Message!.AddReactionAsync(SplitEmoji);

            await TakeUserCurrencyAsync(member.Id, bet);
            _sessions[member.Id] = blackjack;
            
            Log.Debug("Blackjack: Session created");
        }
        
        public bool TryRemoveBlackjack(Snowflake userId, out BlackjackGame? blackjack)
            => _sessions.TryRemove(userId, out blackjack);

        public int GetUserCurrency(Snowflake userId)
            => _gamblingService.GetUserCurrency(userId);

        public Task AddUserCurrencyAsync(Snowflake userId, int currency)
            => _gamblingService.AddUserCurrencyAsync(userId, currency);
        
        public Task TakeUserCurrencyAsync(Snowflake userId, int currency)
            => _gamblingService.RemoveUserCurrencyAsync(userId, currency);
        
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

        private async Task ReactionAddedAsync(ReactionAddedEventArgs args)
        {
            if (!args.Reaction.HasValue)
                return;
            if (!_sessions.TryGetValue(args.User.Id, out var blackjack))
                return;
            if (blackjack.Message!.Id != args.Message.Id)
                return;

            await ProcessGameAsync(blackjack, args.Reaction.Value.Emoji);
        }

        private async Task ReactionRemovedAsync(ReactionRemovedEventArgs args)
        {
            if (!args.Reaction.HasValue)
                return;
            if (!_sessions.TryGetValue(args.User.Id, out var blackjack))
                return;
            if (blackjack.Message!.Id != args.Message.Id)
                return;

            await ProcessGameAsync(blackjack, args.Reaction.Value.Emoji);
        }
        
        private Task ProcessGameAsync(BlackjackGame blackjack, IEmoji emoji)
        {
            
            if (emoji.Equals(CardEmoji))
                _ = RunTaskAsync(blackjack.HitAsync());
        
            if (emoji.Equals(HandEmoji))
                _ = RunTaskAsync(blackjack.StandAsync());
            
            if (emoji.Equals(SplitEmoji) && blackjack.PlayerCanSplit)
                _ = RunTaskAsync(blackjack.SplitAsync());

            return Task.CompletedTask;
        }
    }
}