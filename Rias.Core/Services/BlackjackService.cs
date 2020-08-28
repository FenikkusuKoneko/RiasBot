using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
            RiasBot.Client.MessageReactionAdded += MessageReactionAddedAsync;
            RiasBot.Client.MessageReactionRemoved += MessageReactionRemovedAsync;
        }

        private readonly IEnumerable<(int, string)> _cards;
        private readonly string[] _suits = {"♠", "♥", "♣", "♦"};
        private readonly string[] _highCards = {"A", "J", "Q", "K"};
        
        private readonly ConcurrentDictionary<ulong, BlackjackGame> _sessions = new ConcurrentDictionary<ulong, BlackjackGame>();
        
        public readonly DiscordEmoji CardEmoji = DiscordEmoji.FromUnicode("🎴");
        public readonly DiscordEmoji HandEmoji = DiscordEmoji.FromUnicode("🤚");
        public readonly DiscordEmoji SplitEmoji = DiscordEmoji.FromUnicode("↔");
        
        public bool TryGetBlackjack(ulong userId, out BlackjackGame? blackjack)
            => _sessions.TryGetValue(userId, out blackjack);
        
        public async Task CreateBlackjackAsync(DiscordMember member, DiscordChannel channel, int bet)
        {
            var blackjack = new BlackjackGame(this, member, bet, _suits, _cards);
            await blackjack.CreateAsync(channel);

            await blackjack.Message!.CreateReactionAsync(CardEmoji);
            await blackjack.Message!.CreateReactionAsync(HandEmoji);
            if (blackjack.PlayerCanSplit)
                await blackjack.Message!.CreateReactionAsync(SplitEmoji);

            await TakeUserCurrencyAsync(member.Id, bet);
            _sessions[member.Id] = blackjack;
            
            Log.Debug("Blackjack: Session created");
        }
        
        public bool TryRemoveBlackjack(ulong userId, out BlackjackGame? blackjack)
            => _sessions.TryRemove(userId, out blackjack);

        public Task<int> GetUserCurrencyAsync(ulong userId)
            => _gamblingService.GetUserCurrencyAsync(userId);

        public Task AddUserCurrencyAsync(ulong userId, int currency)
            => _gamblingService.AddUserCurrencyAsync(userId, currency);
        
        public Task TakeUserCurrencyAsync(ulong userId, int currency)
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

        private async Task MessageReactionAddedAsync(MessageReactionAddEventArgs args)
        {
            if (!_sessions.TryGetValue(args.User.Id, out var blackjack))
                return;
            if (blackjack.Message!.Id != args.Message.Id)
                return;

            await ProcessGameAsync(blackjack, args.Emoji);
        }

        private async Task MessageReactionRemovedAsync(MessageReactionRemoveEventArgs args)
        {
            if (!_sessions.TryGetValue(args.User.Id, out var blackjack))
                return;
            if (blackjack.Message!.Id != args.Message.Id)
                return;

            await ProcessGameAsync(blackjack, args.Emoji);
        }
        
        private async Task ProcessGameAsync(BlackjackGame blackjack, DiscordEmoji emoji)
        {
            
            if (emoji.Equals(CardEmoji))
                await RunTaskAsync(blackjack.HitAsync());
        
            if (emoji.Equals(HandEmoji))
                await RunTaskAsync(blackjack.StandAsync());
            
            if (emoji.Equals(SplitEmoji) && blackjack.PlayerCanSplit)
                await RunTaskAsync(blackjack.SplitAsync());
        }
    }
}