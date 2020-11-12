using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Services.Commons
{
    public class BlackjackGame
    { 
        private readonly BlackjackService _service;
        private readonly Queue<(int Value, string Suit)> _deck = new Queue<(int Value, string Suit)>();

        private DiscordMember _member = null!;
        private DiscordEmbedBuilder _embed = null!;
        private DiscordColor _embedColor = new DiscordColor(255, 255, 254);
        private int _bet;
        private int _userCurrency;
        
        private Timer _timer;
        private bool _isEnded;

        private BlackjackHand _playerHand = null!;
        private BlackjackHand? _playerSecondHand;
        private BlackjackHand _houseHand = null!;

        public BlackjackGame(BlackjackService service)
        {
            _service = service;
            _timer = new Timer(_ => TerminateSession(), null, TimeSpan.FromHours(24), TimeSpan.Zero);
        }
        
        public DiscordMessage? Message { get; private set; }
        
        public bool IsRunning { get; private set; }

        private enum HandState
        {
            Playing,
            Standing,
            Bust,
            Blackjack
        }

        private enum WinningState
        {
            Win,
            Lose,
            Push,
            Blackjack
        }
        
        public async Task CreateGameAsync(DiscordMember member, DiscordChannel channel, int bet)
        {
            ShuffleDeck();

            _member = member;
            _embedColor = DiscordColor.White;
            _bet = bet;
            IsRunning = true;
            _isEnded = false;
            _playerSecondHand = null;

            _playerHand = new BlackjackHand();
            _playerHand.Cards.Add(_deck.Dequeue());
            _playerHand.Cards.Add(_deck.Dequeue());
            _playerHand.Process();

            _houseHand = new BlackjackHand();
            _houseHand.Cards.Add(_deck.Dequeue());
            _houseHand.Cards.Add(_deck.Dequeue());
            _houseHand.Process(true);

            _userCurrency = await _service.RemoveUserCurrencyAsync(_member.Id, bet);
            _embed = new DiscordEmbedBuilder
                {
                    Color = _embedColor,
                    Title = _service.GetText(_member.Guild.Id, Localization.GamblingBlackjackTitleCurrency, _bet, _service.Credentials.Currency, _userCurrency),
                    Description = _service.GetText(_member.Guild.Id, Localization.GamblingBlackjackCards, _deck.Count)
                }.AddField($"{_member.Guild.CurrentMember.FullName()} ({_houseHand.Score})", _houseHand.ShowCards(true))
                .AddField($"{_member.FullName()} ({_playerHand.Score})", _playerHand.ShowCards());

            Message = await channel.SendMessageAsync(embed: _embed);

            await Message!.CreateReactionAsync(_service.CardEmoji);
            await Message!.CreateReactionAsync(_service.HandEmoji);
            
            var firstCardValue = _playerHand.Cards[0].Value == 1 ? 11 : Math.Min(_playerHand.Cards[0].Value, 10);
            var secondCardValue = _playerHand.Cards[1].Value == 1 ? 11 : Math.Min(_playerHand.Cards[1].Value, 10);
            
            if (firstCardValue == secondCardValue && _userCurrency >= _bet)
                await Message!.CreateReactionAsync(_service.SplitEmoji);
        }

        public async Task ResumeGameAsync(DiscordMember member, DiscordChannel channel)
        {
            _member = member;
            Message = await channel.SendMessageAsync(embed: _embed);
            await Message!.CreateReactionAsync(_service.CardEmoji);
            await Message!.CreateReactionAsync(_service.HandEmoji);

            var firstCardValue = _playerHand.Cards[0].Value == 1 ? 11 : Math.Min(_playerHand.Cards[0].Value, 10);
            var secondCardValue = _playerHand.Cards[1].Value == 1 ? 11 : Math.Min(_playerHand.Cards[1].Value, 10);

            if (_playerSecondHand is null && firstCardValue == secondCardValue && _userCurrency >= _bet)
                await Message!.CreateReactionAsync(_service.SplitEmoji);
        }

        public Task HitAsync()
        {
            if (_playerHand.HandState == HandState.Playing)
                _playerHand.Cards.Add(_deck.Dequeue());
            else if (_playerSecondHand is not null && _playerSecondHand.HandState == HandState.Playing)
                _playerSecondHand.Cards.Add(_deck.Dequeue());
            
            return ProcessGameAsync();
        }
        
        public async Task StandAsync()
        {
            if (_playerHand.HandState == HandState.Playing)
                _playerHand.Stand();
            else if (_playerSecondHand is not null && _playerSecondHand.HandState == HandState.Playing)
                _playerSecondHand.Stand();

            await ProcessGameAsync();
        }

        public async Task SplitAsync()
        {
            if (_playerSecondHand is not null)
                return;
            
            var firstCardValue = _playerHand.Cards[0].Value == 1 ? 11 : Math.Min(_playerHand.Cards[0].Value, 10);
            var secondCardValue = _playerHand.Cards[1].Value == 1 ? 11 : Math.Min(_playerHand.Cards[1].Value, 10);

            if (firstCardValue != secondCardValue || _userCurrency < _bet)
                return;

            _userCurrency = await _service.RemoveUserCurrencyAsync(_member.Id, _bet);
            _bet *= 2;
            _embed.WithTitle(_service.GetText(_member.Guild.Id, Localization.GamblingBlackjackTitleCurrency, _bet, _service.Credentials.Currency, _userCurrency));
            
            _playerSecondHand = new BlackjackHand();
            _playerSecondHand.Cards.Add(_playerHand.Cards[1]);
            _playerHand.Cards[1] = _deck.Dequeue();
            _playerSecondHand.Cards.Add(_deck.Dequeue());
            
            if (CheckManageMessagesPermission())
                await Message!.DeleteReactionsEmojiAsync(_service.SplitEmoji);

            await ProcessGameAsync();
        }

        public void StopGame()
        {
            IsRunning = false;
            if (_isEnded)
                TerminateSession();
            else
                IsRunning = false;
        }

        private async Task ProcessGameAsync()
        {
            var gameEnded = false;

            if (_playerHand.HandState == HandState.Playing)
                _playerHand.Process();
            if (_playerSecondHand is not null && _playerSecondHand.HandState == HandState.Playing)
                _playerSecondHand.Process();

            string? description = null;

            if (_playerHand.HandState != HandState.Playing && (_playerSecondHand is null || _playerSecondHand.HandState != HandState.Playing))
            {
                gameEnded = true;
                var winning = _bet;
                var betPerHand = _playerSecondHand is null ? _bet : _bet / 2;

                if ((_playerHand.HandState != HandState.Bust && _playerSecondHand is null)
                    || (_playerSecondHand is not null && _playerSecondHand.HandState != HandState.Bust))
                {
                    _houseHand.Process();
                    while (_houseHand.HandState == HandState.Playing)
                    {
                        if (_houseHand.Score < 17 || _houseHand.Score == 17 && _houseHand.Cards.Count(x => x.Value == 1) == 1)
                            _houseHand.Cards.Add(_deck.Dequeue());
                        else if (_houseHand.HandState == HandState.Playing)
                            _houseHand.Stand();
                    
                        _houseHand.Process();
                    }
                    
                    _embed.Fields[0].Name = $"{_member.Guild.CurrentMember.FullName()} ({_houseHand.Score})";
                    _embed.Fields[0].Value = _houseHand.ShowCards();
                }

                _playerHand.ProcessWinning(_houseHand);
                _playerSecondHand?.ProcessWinning(_houseHand);

                switch (_playerHand.WinningState)
                {
                    case WinningState.Win:
                        winning += betPerHand;
                        break;
                    case WinningState.Blackjack:
                        winning += betPerHand * 3 / 2; // the blackjack payoff is 3/2
                        if (_playerSecondHand is null)
                        {
                            _embedColor = RiasUtilities.Green;
                            description = _service.GetText(_member.Guild.Id, Localization.GamblingBlackjackBlackjack, winning, _service.Credentials.Currency, _userCurrency + winning);
                        }
                        break;
                    case WinningState.Push:
                        if (_playerSecondHand is null)
                        {
                            _embedColor = DiscordColor.Yellow;
                            description = _service.GetText(_member.Guild.Id, Localization.GamblingBlackjackPush, _userCurrency + winning, _service.Credentials.Currency);
                        }
                        break;
                    case WinningState.Lose:
                        winning -= betPerHand;
                        break;
                }

                if (_playerSecondHand is not null)
                {
                    switch (_playerSecondHand.WinningState)
                    {
                        case WinningState.Win:
                            winning += betPerHand;
                            break;
                        case WinningState.Blackjack:
                            winning += betPerHand * 3 / 2;
                            break;
                        case WinningState.Lose:
                            winning -= betPerHand;
                            break;
                    }
                }
                
                if (winning > 0)
                    _userCurrency = await _service.AddUserCurrencyAsync(_member.Id, winning);

                if (string.IsNullOrEmpty(description))
                {
                    winning -= _bet;
                    if (winning > 0)
                    {
                        _embedColor = RiasUtilities.Green;
                        description = _service.GetText(_member.Guild.Id, Localization.GamblingYouWon, winning, _service.Credentials.Currency, _userCurrency);
                    }
                    else if (winning < 0)
                    {
                        _embedColor = RiasUtilities.Red;
                        description = _service.GetText(_member.Guild.Id, Localization.GamblingYouLost, Math.Abs(winning), _service.Credentials.Currency, _userCurrency);
                    }
                    else if (_playerSecondHand != null)
                    {
                        _embedColor = RiasUtilities.Yellow;
                        description = _service.GetText(_member.Guild.Id, Localization.GamblingBlackjackTie, _userCurrency, _service.Credentials.Currency);
                    }
                }
            }

            var firstHandFieldName = $"{_member.FullName()} ({_playerHand.Score})";
            if (_playerHand.HandState == HandState.Playing && _playerSecondHand is not null)
                firstHandFieldName = "▶ " + firstHandFieldName;
            
            _embed.Fields[1].Name = firstHandFieldName;
            _embed.Fields[1].Value = _playerHand.ShowCards();

            if (_playerSecondHand is not null)
            {
                if (_embed.Fields.Count == 2)
                    _embed.AddField("\u200B", "\u200B");

                var secondHandFieldName = $"{_member.FullName()} ({_playerSecondHand.Score})";
                if (_playerHand.HandState != HandState.Playing && _playerSecondHand.HandState == HandState.Playing)
                    secondHandFieldName = "▶ " + secondHandFieldName;
                
                _embed.Fields[2].Name = secondHandFieldName;
                _embed.Fields[2].Value = _playerSecondHand.ShowCards();
            }

            if (gameEnded)
            {
                _embed.WithTitle(_service.GetText(_member.Guild.Id, Localization.GamblingBlackjackTitle, _bet, _service.Credentials.Currency));
                _embed.WithDescription(description);
                StopGame();

                if (CheckManageMessagesPermission())
                    await Message!.DeleteAllReactionsAsync();
            }
            else
            {
                _embed.WithDescription(_service.GetText(_member.Guild.Id, Localization.GamblingBlackjackCards, _deck.Count));
            }

            _embed.WithColor(_embedColor);
            await Message!.ModifyAsync(embed: _embed.Build());
        }

        private void ShuffleDeck()
        {
            if (_deck.Count > 104)
                return;
            
            _deck.Clear();
            var random = new Random();
            var suitsArray = new[] {_service.SpadesEmoji, _service.HeartsEmoji, _service.ClubsEmoji, _service.DiamondsEmoji};
            
            for (var i = 0; i < 4; i++)
            {
                var randomDeck = new Queue<(int, string)>((from suit in suitsArray from card in InitializeDeck() select (card, suit))
                    .OrderBy(_ => random.Next()));
                
                foreach (var card in randomDeck)
                    _deck.Enqueue(card);
            }
        }
        
        private IEnumerable<int> InitializeDeck()
        {
            for (var i = 1; i <= 14; i++)
                if (i != 11)
                    yield return i;
        }

        private void TerminateSession()
        {
            if (IsRunning)
            {
                _isEnded = true;
                return;
            }
            
            _service.RemoveSession(_member);
        }

        private bool CheckManageMessagesPermission()
        {
            var currentMember = _member.Guild.CurrentMember;
            var channel = Message!.Channel;
                
            var channelPermissions = currentMember.PermissionsIn(channel);
            var channelManageMessagesPerm = channelPermissions.HasPermission(Permissions.ManageMessages);
            var guildManageMessagesPerm = currentMember.GetPermissions().HasPermission(Permissions.ManageMessages);

            if (channelManageMessagesPerm && guildManageMessagesPerm)
                return true;
            
            return channelManageMessagesPerm;
        }

        private class BlackjackHand
        {
            public readonly List<(int Value, string Suit)> Cards = new List<(int Value, string Suit)>();
            
            public HandState HandState { get; private set; }
            public WinningState WinningState { get; private set; }
            
            public int Score { get; private set; }

            public void Process(bool isHouse = false)
            {
                var score = 0;
                if (!isHouse)
                {
                    foreach (var (value, _) in Cards.Where(x => x.Value != 1))
                        score += Math.Min(value, 10);

                    foreach (var (value, _) in Cards.Where(x => x.Value == 1))
                        score += score + value > 21 ? 1 : 11;
                }
                else
                {
                    score = Cards[0].Value == 1 ? 11 : Math.Min(Cards[0].Value, 10);
                }
                
                Score = score;
                
                if (score == 21)
                    HandState = HandState.Blackjack;
                else if (score > 21)
                    HandState = HandState.Bust;
            }

            public void ProcessWinning(BlackjackHand houseHand)
            {
                switch (HandState)
                {
                    case HandState.Standing when houseHand.HandState == HandState.Bust || Score > houseHand.Score:
                        WinningState = WinningState.Win;
                        break;
                    case HandState.Standing when houseHand.HandState == HandState.Standing && Score == houseHand.Score:
                    case HandState.Blackjack when houseHand.HandState == HandState.Blackjack:
                        WinningState = WinningState.Push;
                        break;
                    case HandState.Blackjack when houseHand.HandState != HandState.Blackjack:
                        WinningState = WinningState.Blackjack;
                        break;
                    default:
                        WinningState = WinningState.Lose;
                        break;
                }
            }

            public void Stand()
            {
                if (HandState == HandState.Playing)
                    HandState = HandState.Standing;
            }

            public string ShowCards(bool isHouse = false)
                => isHouse ? $"{StringifyCard(Cards[0])}  🎴" : string.Join("  ", Cards.Select(StringifyCard));

            private string StringifyCard((int Value, string Suit) card)
                => card.Value switch
                {
                    1 => $"A{card.Suit}",
                    12 => $"J{card.Suit}",
                    13 => $"Q{card.Suit}",
                    14 => $"K{card.Suit}",
                    _ => $"{card.Value}{card.Suit}"
                };
        }
    }
}