using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Services.Commons
{
    public class BlackjackGame
    {
        public readonly CachedMember Member;
        public IUserMessage? Message { get; private set; }
        public bool PlayerCanSplit { get; private set; }

        private readonly BlackjackService _service;
        private readonly LocalEmbedBuilder _embed;

        private readonly Queue<(int, string, string)> _deck;
        private readonly string _arrowIndex = "➡";
        private readonly ulong _guildId;
        private int _bet;

        private readonly BlackjackHand _playerFirstHand;
        private BlackjackHand? _playerSecondHand;
        private readonly BlackjackHand _houseHand;
        
        public BlackjackGame(BlackjackService service, CachedMember member, int bet, IEnumerable<string> suits, IEnumerable<(int Value, string Card)> cards)
        {
            Member = member;
            _service = service;
            _embed = new LocalEmbedBuilder();
            _deck = new Queue<(int, string, string)>();
            _guildId = member.Guild.Id;
            _bet = bet;

            var suitsArray = suits.ToArray();
            var cardsArray = cards.ToArray();
            
            var random = new Random();
            
            for (var i = 0; i < 4; i++)
            {
                var randomDeck = new Queue<(int, string, string)>((from suit in suitsArray from card in cardsArray select (card.Value, card.Card, suit))
                    .OrderBy(_ => random.Next()));
                foreach (var card in randomDeck)
                {
                    _deck.Enqueue(card);
                }
            }
            
            _playerFirstHand = new BlackjackHand().AddCard(_deck.Dequeue()).AddCard(_deck.Dequeue());
            _houseHand = new BlackjackHand().AddCard(_deck.Dequeue()).AddCard(_deck.Dequeue());
        }
        
        public async Task CreateAsync(CachedTextChannel channel)
        {
            _embed.WithColor(RiasUtilities.Yellow)
                .WithTitle($"{_service.GetText(_guildId, Localization.GamblingBlackjack)} | {_service.GetText(_guildId, Localization.GamblingBet)}: {_bet} {_service.Credentials.Currency}")
                .AddField($"{Member} ({_playerFirstHand.Score})", _playerFirstHand.ShowCards());

            var playerCards = _playerFirstHand.Cards;
            if (Math.Min(playerCards[0].Value, 10) == Math.Min(playerCards[1].Value, 10)
                && _service.GetUserCurrency(Member.Id) >= _bet)
                PlayerCanSplit = true;

            var houseFirstCard = _houseHand.Cards[0].Value;
            if (houseFirstCard > 10)
                houseFirstCard = 10;
            if (houseFirstCard == 1)
                houseFirstCard = 11;
            
            _embed.AddField($"{Member.Guild.CurrentMember} ({houseFirstCard})", _houseHand.ShowFirstCard("🎴"));

            Message = await channel.SendMessageAsync(_embed);
        }
        
        public async Task ResendMessageAsync(CachedTextChannel channel)
        {
            Message = await channel.SendMessageAsync(_embed);
            await Message!.AddReactionAsync(_service.CardEmoji);
            await Message!.AddReactionAsync(_service.HandEmoji);
            if (PlayerCanSplit)
                await Message!.AddReactionAsync(_service.SplitEmoji);
        }
        
        public async Task HitAsync()
        {
            var hand = PlayerTurn();
            switch (hand.State)
            {
                case BlackjackHand.HandState.Playing:
                    if (PlayerCanSplit)
                    {
                        PlayerCanSplit = false;
                        await Message!.RemoveMemberReactionAsync(Member.Id, _service.SplitEmoji);
                        await Message!.RemoveOwnReactionAsync(_service.SplitEmoji);
                    }
                    await Message!.ModifyAsync(x => x.Embed = EditEmbed().Build());
                    break;
                case BlackjackHand.HandState.Blackjack:
                case BlackjackHand.HandState.Bust:
                    if (_playerSecondHand is null)
                        await GameOverAsync();
                    if (hand != _playerSecondHand)
                        await Message!.ModifyAsync(x => x.Embed = EditEmbed().Build());
                    else
                        await GameOverAsync();
                    return;
            }
        }
        
        public async Task StandAsync()
        {
            var currentHand = GetCurrentHand();
            GetCurrentHand().State = BlackjackHand.HandState.Standing;
            if (_playerSecondHand is null)
                await GameOverAsync();
            if (currentHand != _playerSecondHand)
                await Message!.ModifyAsync(x => x.Embed = EditEmbed().Build()); 
            else
                await GameOverAsync();
        }
        
        public async Task SplitAsync()
        {
            PlayerCanSplit = false;
            await Message!.RemoveMemberReactionAsync(Member.Id, _service.SplitEmoji);
            await Message!.RemoveOwnReactionAsync(_service.SplitEmoji);
            
            var card = _playerFirstHand.RemoveLastCard();
            _playerFirstHand.AddCard(_deck.Dequeue());
            _playerSecondHand = new BlackjackHand().AddCard(card).AddCard(_deck.Dequeue());
            
            await _service.TakeUserCurrencyAsync(Member.Id, _bet);
            _bet += _bet;

            EditEmbed().Title = $"{_service.GetText(_guildId, Localization.GamblingBlackjack)} | {_service.GetText(_guildId, Localization.GamblingBet)}: {_bet} {_service.Credentials.Currency}";
            await Message!.ModifyAsync(x => x.Embed = EditEmbed().Build());
        }
        
        private BlackjackHand GetCurrentHand()
            => _playerSecondHand is null || _playerFirstHand.State == BlackjackHand.HandState.Playing ? _playerFirstHand : _playerSecondHand;
        
        private BlackjackHand PlayerTurn()
        {
            var currentHand = GetCurrentHand();
            currentHand.AddCard(_deck.Dequeue());
            if (currentHand.Score == 21)
                currentHand.State = BlackjackHand.HandState.Blackjack;
            if (currentHand.Score > 21)
                currentHand.State = BlackjackHand.HandState.Bust;
            return currentHand;
        }
        
        private void DealerTurn()
        {
            if (_houseHand.Score < 17)
                _houseHand.AddCard(_deck.Dequeue(), true);
            else
                _houseHand.State = _houseHand.Score < 21 ? BlackjackHand.HandState.Standing : BlackjackHand.HandState.Bust;
        }
        
        private LocalEmbedBuilder EditEmbed(bool showHouseCards = false)
        {
            var index = _playerSecondHand != null && _playerFirstHand.State == BlackjackHand.HandState.Playing ? _arrowIndex : null;
            var playerField = _embed.Fields[0];
            playerField.Name = $"{index}{Member} ({_playerFirstHand.Score})";
            playerField.Value = _playerFirstHand.ShowCards();

            if (_playerSecondHand != null)
            {
                index = _playerFirstHand.State != BlackjackHand.HandState.Playing && _playerSecondHand.State == BlackjackHand.HandState.Playing ? _arrowIndex : null;
                var playerSecondField = _embed.Fields.Count == 2 ? new LocalEmbedFieldBuilder() : _embed.Fields[1];

                playerSecondField.Name = $"{index}{Member} ({_playerSecondHand.Score})";
                playerSecondField.Value = _playerSecondHand.ShowCards();
                
                if (_embed.Fields.Count == 2)
                    _embed.Fields.Insert(1, playerSecondField);
            }

            if (showHouseCards)
            {
                var dealerField = _embed.Fields[^1];
                dealerField.Name = $"{Member.Guild.CurrentMember} ({_houseHand.Score})";
                dealerField.Value = _houseHand.ShowCards();
            }

            return _embed;
        }
        
        private async Task GameOverAsync(bool busted = false)
        {
            await Message!.ClearReactionsAsync();
            if (!busted)
            {
                while (_houseHand.State == BlackjackHand.HandState.Playing)
                    DealerTurn();
            }

            var embed = EditEmbed(!busted);
            var win = AnalyzeWinning();

            embed.Color = win > 0 ? RiasUtilities.Green : RiasUtilities.Red;
            embed.Description = win == 0
                ? _service.GetText(_guildId, Localization.GamblingBlackjackDraw, _service.Credentials.Currency)
                : _service.GetText(_guildId, win > 0 ? Localization.GamblingYouWon : Localization.GamblingYouLost, Math.Abs(win), _service.Credentials.Currency);

            await Message.ModifyAsync(x => x.Embed = embed.Build());
            
            if (win > 0)
                await _service.AddUserCurrencyAsync(Member.Id, win + _bet);
            
            _service.TryRemoveBlackjack(Member.Id, out _);
        }
        
        private int AnalyzeWinning()
        {
            var win = 0;
            var betPerHand = _playerSecondHand is null ? _bet : _bet / 2;
            
            CalculateHandWin(_playerFirstHand);
            
            if (_playerSecondHand != null)
                CalculateHandWin(_playerSecondHand);

            void CalculateHandWin(BlackjackHand hand)
            {
                if (hand.Score > 21)
                    win -= betPerHand;
                else if (hand.Score > _houseHand.Score || _houseHand.Score > 21)
                    win += betPerHand;
                else
                    win -= betPerHand;
            }
            
            return win;
        }
    }
    
    public class BlackjackHand
    {
        public readonly List<(int Value, string Card, string Suit)> Cards = new List<(int, string, string)>();
        public int Score { get; private set; }
        public HandState State { get; set; }

        private bool _softHand;
        private readonly StringBuilder _handString = new StringBuilder();

        public BlackjackHand AddCard((int Value, string Card, string Suit) card, bool ignoreSoftHand = false)
        {
            Cards.Add(card);
            _handString.Append($"{card.Card}{card.Suit}");

            var cardValue = card.Value;
            if (cardValue != 1)
            {
                Score += Math.Min(cardValue, 10);
                return this;
            }
            
            if (!_softHand && Score + 11 <= 21)
            {
                Score += 11;
                _softHand = true;
                return this;
            }

            if (_softHand && !ignoreSoftHand)
            {
                // take 10 (A(11) - 1), add A(1)
                Score -= 9;
                _softHand = false;
                return this;
            }

            Score += 1;
            return this;
        }

        public (int, string, string) RemoveLastCard()
        {
            var card = Cards[^1];
            Cards.RemoveAt(Cards.Count - 1);
            
            var cardValue = card.Value;
            Score -= Math.Min(cardValue, 10);

            if (Cards.Count == 1 && Cards[0].Value == 1)
                Score += 10;

            _handString.Remove(_handString.Length - 2, 2);
            return card;
        }

        public string ShowCards()
            => _handString.ToString();

        public string ShowFirstCard(string value)
        {
            var sb = new StringBuilder();
            sb.Append(Cards[0].Card).Append(Cards[0].Suit);

            for (var i = 1; i < Cards.Count; i++)
            {
                sb.Append(value);
            }

            return sb.ToString();
        }

        public enum HandState
        {
            Playing,
            Standing,
            Bust,
            Blackjack
        }
    }
}