using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Services.Commons
{
    public class BlackjackGame
    {
        public readonly SocketGuildUser User;
        public IUserMessage? Message { get; private set; }
        public bool PlayerCanSplit { get; private set; }

        private readonly BlackjackService _service;
        private readonly EmbedBuilder _embed;

        private readonly Queue<(int, string, string)> _deck;
        private readonly string _arrowIndex = "➡";
        private readonly ulong _guildId;
        private int _bet;

        private readonly BlackjackHand _playerFirstHand;
        private BlackjackHand? _playerSecondHand;
        private readonly BlackjackHand _houseHand;

        public BlackjackGame(BlackjackService service, SocketGuildUser user, int bet, IEnumerable<string> suits, IEnumerable<(int Value, string Card)> cards)
        {
            User = user;
            _service = service;
            _embed = new EmbedBuilder();
            _deck = new Queue<(int, string, string)>();
            _guildId = user.Guild.Id;
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

        public async Task CreateAsync(IMessageChannel channel)
        {
            _embed.WithColor(RiasUtils.Yellow)
                .WithTitle($"{_service.GetText(_guildId, "Blackjack")} | {_service.GetText(_guildId, "Bet")}: {_bet} {_service.Creds.Currency}")
                .AddField($"{User} ({_playerFirstHand.Score})", _playerFirstHand.ShowCards());

            var playerCards = _playerFirstHand.Cards;
            if (Math.Min(playerCards[0].Value, 10) == Math.Min(playerCards[1].Value, 10)
                && _service.GetUserCurrency(User) >= _bet)
                PlayerCanSplit = true;

            var houseFirstCard = _houseHand.Cards[0].Value;
            if (houseFirstCard > 10)
                houseFirstCard = 10;
            if (houseFirstCard == 1)
                houseFirstCard = 11;
            
            _embed.AddField($"{User.Guild.CurrentUser} ({houseFirstCard})", _houseHand.ShowFirstCard("🎴"));

            Message = await channel.SendMessageAsync(_embed);
        }

        public async Task ResendMessageAsync(IMessageChannel channel)
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
                        await Message!.RemoveReactionAsync(_service.SplitEmoji, User);
                        await Message!.RemoveReactionAsync(_service.SplitEmoji, User.Guild.CurrentUser);
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
            await Message!.RemoveReactionAsync(_service.SplitEmoji, User);
            await Message!.RemoveReactionAsync(_service.SplitEmoji, User.Guild.CurrentUser);
            
            var card = _playerFirstHand.RemoveLastCard();
            _playerFirstHand.AddCard(_deck.Dequeue());
            _playerSecondHand = new BlackjackHand().AddCard(card).AddCard(_deck.Dequeue());
            
            await _service.TakeUserCurrencyAsync(User, _bet);
            _bet += _bet;

            EditEmbed().Title = $"{_service.GetText(_guildId, "Blackjack")} | {_service.GetText(_guildId, "Bet")}: {_bet} {_service.Creds.Currency}";
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

        private EmbedBuilder EditEmbed(bool showHouseCards = false)
        {
            var index = _playerSecondHand != null && _playerFirstHand.State == BlackjackHand.HandState.Playing ? _arrowIndex : null;
            var playerField = _embed.Fields[0];
            playerField.Name = $"{index}{User} ({_playerFirstHand.Score})";
            playerField.Value = _playerFirstHand.ShowCards();

            if (_playerSecondHand != null)
            {
                index = _playerFirstHand.State != BlackjackHand.HandState.Playing && _playerSecondHand.State == BlackjackHand.HandState.Playing ? _arrowIndex : null;
                var playerSecondField = _embed.Fields.Count == 2 ? new EmbedFieldBuilder() : _embed.Fields[1];

                playerSecondField.Name = $"{index}{User} ({_playerSecondHand.Score})";
                playerSecondField.Value = _playerSecondHand.ShowCards();
                
                if (_embed.Fields.Count == 2)
                    _embed.Fields.Insert(1, playerSecondField);
            }

            if (showHouseCards)
            {
                var dealerField = _embed.Fields[^1];
                dealerField.Name = $"{User.Guild.CurrentUser} ({_houseHand.Score})";
                dealerField.Value = _houseHand.ShowCards();
            }

            return _embed;
        }

        private async Task GameOverAsync(bool busted = false)
        {
            await Message!.RemoveAllReactionsAsync();
            if (!busted)
            {
                while (_houseHand.State == BlackjackHand.HandState.Playing)
                    DealerTurn();
            }

            var embed = EditEmbed(!busted);
            var win = AnalyzeWinning();

            var description = string.Empty;
            if (win > 0)
            {
                description = "YouWon";
                embed.Color = RiasUtils.Green;
            }
            else if (win < 0)
            {
                description = "YouLost";
                embed.Color = RiasUtils.Red;
            }

            if (win == 0)
                embed.Description = _service.GetText(_guildId, "BlackjackDraw", _service.Creds.Currency);
            else
                embed.Description = _service.GetText(_guildId, description, Math.Abs(win), _service.Creds.Currency);

            await Message.ModifyAsync(x => x.Embed = embed.Build());
            if (win > 0) await _service.AddUserCurrencyAsync(User, win + _bet);
            
            _service.TryRemoveBlackjack(User, out _);
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