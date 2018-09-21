using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Modules.Gambling.Services;

namespace RiasBot.Modules.Gambling.Commons
{
    public class BlackjackGame
    {
        private readonly BlackjackService _service;
        
        public BlackjackGame(BlackjackService service)
        {
            _service = service;
        }

        private IMessageChannel _channel;
        private IGuildUser _user;
        private IUserMessage _message;
        private IGuildUser _bot;

        private IEmote _hit;
        private IEmote _stand;
        private IEmote _split;

        private bool _gameEnded;
        private bool _ignoreReactions;
        private bool _userHasSplit;
        private bool _optionToSplit;
        private int _splitHandPosition;
        private int _bet;
        
        public bool ManageMessagesPermission = true;

        private PlayerHand _userHand;
        private PlayerHand _house;

        private List<PlayerHand> _userSplitHands;

        private List<Card> _deck;

        private class PlayerHand
        {
            public PlayerHandState HandState { get; set; }
            public List<Card> Cards { get; set; }
            public PlayerStatus Status { get; set; }
        }

        private readonly List<string> _suits = new List<string>
        {
            "♠",
            "♥",
            "♣",
            "♦"
        };
        
        private readonly List<string> _highCards = new List<string>
        {
            "J",
            "Q",
            "K"
        };
        
        private class Card
        {
            public int Number { get; set; }
            public string NumberString { get; set; }
            public string Type { get; set; }
        }

        public async Task InitializeGameAsync(IMessageChannel channel, IGuildUser user, int bet)
        {
            _channel = channel;
            _user = user;
            _bot = _service.Client.GetGuild(user.GuildId).CurrentUser;
            
            _hit = new Emoji("🎴");
            _stand = new Emoji("🤚");
            _split = new Emoji("↔");

            _bet = bet;
            await TakeBet().ConfigureAwait(false);

            _userHand = new PlayerHand
            {
                HandState = PlayerHandState.Lower,
                Cards = new List<Card>(),
                Status = PlayerStatus.Playing
            };
            _house = new PlayerHand
            {
                HandState = PlayerHandState.Lower,
                Cards = new List<Card>(),
                Status = PlayerStatus.Playing
            };

            InitializeDeck();

            _userHand.Cards.Add(DrawCardFromDeck());
            _userHand.Cards.Add(DrawCardFromDeck());
            _house.Cards.Add(DrawCardFromDeck());
            _house.Cards.Add(DrawCardFromDeck());
            await AnalyzeHandAsync();
            
            await AddReactionsAsync().ConfigureAwait(false);
        }
        
        public async Task ResumeGameAsync(IGuildUser user, IMessageChannel channel)
        {
            _channel = channel;
            _user = user;
            
            if (_bot.GuildPermissions.ManageMessages)
                await _message.DeleteAsync().ConfigureAwait(false);
            _message = null;
            _bot = _service.Client.GetGuild(user.GuildId).CurrentUser;
            
            if (!_userHasSplit)
                await AnalyzeHandAsync();
            else
                await AnalyzeSplitHandAsync();
            
            await AddReactionsAsync().ConfigureAwait(false);
        }

        public async Task UpdateGameAsync(SocketReaction reaction)
        {
            if (reaction.MessageId != _message.Id)
                return;
            
            if (reaction.UserId == _user.Id)
            {
                if (!_ignoreReactions)
                { 
                    if (reaction.Emote.Equals(_hit))
                    {
                        if (!_userHasSplit)
                        {
                            UserTurn(_userHand);
                            await AnalyzeHandAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            UserTurn(_userSplitHands[_splitHandPosition]);
                            await AnalyzeSplitHandAsync().ConfigureAwait(false);
                        }
                    }
                    else if (reaction.Emote.Equals(_stand))
                    {
                        if (!_userHasSplit)
                        {
                            _userHand.Status = PlayerStatus.Standing;
                            await AnalyzeHandAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            _userSplitHands[_splitHandPosition].Status = PlayerStatus.Standing;
                            await AnalyzeSplitHandAsync().ConfigureAwait(false);
                        }
                    }
                    else if (reaction.Emote.Equals(_split))
                    {
                        if (!_userHasSplit)
                        {
                            _userHasSplit = true;
                            await TakeBet();
                            _bet *= 2;
                            
                            SplitUserHand();
                            await AnalyzeSplitHandAsync().ConfigureAwait(false);
                            await _message.RemoveReactionAsync(_split, _bot).ConfigureAwait(false);
                        }
                    }
                }
            }

            if (_bot.GuildPermissions.ManageMessages)
            {
                if (reaction.UserId != _bot.Id)
                    await _message.RemoveReactionAsync(reaction.Emote, reaction.User.Value).ConfigureAwait(false);
                ManageMessagesPermission = true;
            }
            else
            {
                ManageMessagesPermission = false;
            }
        }
        
        private async Task AnalyzeHandAsync()
        {
            if (_userHand.Cards.Count == 2)
            {
                var firstCardNumber = _userHand.Cards[0].Number;
                var secondCardNumber = _userHand.Cards[1].Number;

                if (firstCardNumber > 10)
                    firstCardNumber = 10;
                if (secondCardNumber > 10)
                    secondCardNumber = 10;
                if (firstCardNumber == secondCardNumber)
                {
                    if (_bet <= _service.GetCurrency(_user))
                    {
                        _optionToSplit = true;
                    }
                }
            }
            var win = false;
            var gameEnded = false;
            if (_userHand.Status == PlayerStatus.Standing)
            {
                _ignoreReactions = true;

                while (_house.Status == PlayerStatus.Playing)
                {
                    DealerTurn();
                }

                gameEnded = true;
            }

            var userScore = UpdateUserHand(_userHand);
            var houseScore = UpdateHouse();

            if (_userHand.HandState == PlayerHandState.Blackjack && _house.HandState == PlayerHandState.Blackjack)
            {
                await UpdateGameMessageAsync(userScore, houseScore, "Push", true).ConfigureAwait(false);
                await GivePrize(_bet / 2).ConfigureAwait(false);
            }
            else if (_userHand.HandState == PlayerHandState.Blackjack)
            {
                await UpdateGameMessageAsync(userScore, houseScore, "You win! You have Blackjack!", true).ConfigureAwait(false);
                win = true;
            }
            //else if (_house.HandState == PlayerHandState.Blackjack)
            //    await UpdateGameMessageAsync(userScore, houseScore, "Bust! The house has Blackjack!", true).ConfigureAwait(false);
            else if (_userHand.HandState == PlayerHandState.Lower)
            {
                if (_house.HandState == PlayerHandState.Higher)
                {
                    await UpdateGameMessageAsync(userScore, houseScore, "You win!", true).ConfigureAwait(false);
                    win = true;
                }
                else
                {
                    if (!gameEnded)
                    {
                        houseScore = GetHouseFirstCardNumber();
                        await UpdateGameMessageAsync(userScore, houseScore, null).ConfigureAwait(false);
                        return;
                    }

                    if (userScore > houseScore)
                    {
                        await UpdateGameMessageAsync(userScore, houseScore, "You win!", true).ConfigureAwait(false);
                        win = true;
                    }
                    else if (userScore < houseScore)
                        await UpdateGameMessageAsync(userScore, houseScore, "Bust!", true).ConfigureAwait(false);
                    else
                    {
                        await UpdateGameMessageAsync(userScore, houseScore, "Push", true).ConfigureAwait(false);
                        await GivePrize(_bet / 2).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await UpdateGameMessageAsync(userScore, houseScore, "Bust!", true).ConfigureAwait(false);
            }
            if (win)
                await GivePrize(_bet * 2).ConfigureAwait(false);
            await StopGameAsync(_user).ConfigureAwait(false);
        }
        
        private async Task AnalyzeSplitHandAsync()
        {
            var firstHand = _userSplitHands[0];
            var secondHand = _userSplitHands[1];

            var userFirstHandScore = UpdateUserHand(firstHand);
            var userSecondHandScore = UpdateUserHand(secondHand);
            var houseScore = UpdateHouse();

            if (firstHand.Status == PlayerStatus.Standing)
                _splitHandPosition = 1;
            if (secondHand.Status == PlayerStatus.Standing)
                _splitHandPosition = -1;
            
            if (secondHand.Status == PlayerStatus.Standing)
            {
                _ignoreReactions = true;

                if (firstHand.HandState == PlayerHandState.Lower || secondHand.HandState == PlayerHandState.Lower)
                {
                    while (_house.Status == PlayerStatus.Playing)
                    {
                        DealerTurn();
                    }
                }

                _gameEnded = true;
                houseScore = UpdateHouse();
            }

            if (!_gameEnded)
            {
                if (_userSplitHands[_splitHandPosition].Cards.Count == 1)
                    _userSplitHands[_splitHandPosition].Cards.Add(DrawCardFromDeck());
                if (_splitHandPosition == 0)
                    userFirstHandScore = UpdateUserHand(firstHand);
                else
                    userSecondHandScore = UpdateUserHand(secondHand);
                houseScore = GetHouseFirstCardNumber();
                
                await UpdateGameSplitMessageAsync(userFirstHandScore, userSecondHandScore, houseScore, _splitHandPosition, null, null, null);
            }
            else
            {
                var handsMessages = new string[2];
                var prize = 0;
                for (var i = 0; i < 2; i++)
                {
                    var userHand = _userSplitHands[i];
                    if (userHand.HandState == PlayerHandState.Blackjack && _house.HandState == PlayerHandState.Blackjack)
                    {
                        handsMessages[i] = "Push!";
                        prize += _bet / 2 / 2;
                    }
                    else if (userHand.HandState == PlayerHandState.Blackjack)
                    {
                        handsMessages[i] = "Win! Blackjack!";
                        prize += _bet;
                    }
                    else if (_house.HandState == PlayerHandState.Blackjack)
                        handsMessages[i] = "Bust!";
                    else if (userHand.HandState == PlayerHandState.Lower)
                    {
                        if (_house.HandState == PlayerHandState.Higher)
                        {
                            handsMessages[i] = "Win!";
                            prize += _bet;
                        }
                        else
                        {
                            var userScore = i == 0 ? userFirstHandScore : userSecondHandScore;
                            if (userScore > houseScore)
                            {
                                handsMessages[i] = "Win!";
                                prize += _bet;
                            }
                            else if (userScore < houseScore)
                                handsMessages[i] = "Bust!";
                            else
                            {
                                handsMessages[i] = "Push!";
                                prize += _bet / 2 / 2;
                            }
                        }
                    }
                    else
                    {
                        handsMessages[i] = "Bust!";
                    }
                }
                await UpdateGameSplitMessageAsync(userFirstHandScore, userSecondHandScore, houseScore, _splitHandPosition, null, handsMessages[0], handsMessages[1], true);
                await GivePrize(prize).ConfigureAwait(false);
                await StopGameAsync(_user).ConfigureAwait(false);
            }
        }
        
        private async Task UpdateGameMessageAsync(int userScore, int houseScore, string description, bool gameEnded = false)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithAuthor($"Blackjack | Bet: {_bet}", _user.GetRealAvatarUrl());

            var userCardsString = CardsString(_userHand.Cards).ToList();
            var houseCardsString = CardsString(_house.Cards).ToList();

            if (!gameEnded)
            {
                for (var i = 1; i < houseCardsString.Count; i++)
                {
                    houseCardsString[i] = "🎴";
                }
            }

            if (description != null)
                embed.WithDescription(description);

            embed.AddField($"{_bot.Username} ({houseScore})", string.Join(" ", houseCardsString))
                .AddField($"You ({userScore})", string.Join(" ", userCardsString));
                
            if (_message is null)
                _message = await _channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
            else
                await _message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            if (_optionToSplit)
            {
                await _message.AddReactionAsync(_split).ConfigureAwait(false);
                _optionToSplit = false;
            }
        }
        
        private async Task UpdateGameSplitMessageAsync(int firstHandScore, int secondHandScore, int houseScore, int splitHandPosition, string description,
            string firstHandStateString, string secondHandStateString, bool gameEnded = false)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithAuthor($"Blackjack | Bet: {_bet} (split)", _user.GetRealAvatarUrl());

            var userFirstHandCardsString = CardsString(_userSplitHands[0].Cards).ToList();
            var userSecondHandCardsString = CardsString(_userSplitHands[1].Cards).ToList();
            var houseCardsString = CardsString(_house.Cards).ToList();

            if (!gameEnded)
            {
                for (var i = 1; i < houseCardsString.Count; i++)
                {
                    houseCardsString[i] = "🎴";
                }
            }

            if (description != null)
                embed.WithDescription(description);

            if (!string.IsNullOrEmpty(firstHandStateString))
                firstHandStateString = $" [{firstHandStateString}]";
            if (!string.IsNullOrEmpty(secondHandStateString))
                secondHandStateString = $" [{secondHandStateString}]";

            embed.AddField($"{_bot.Username} ({houseScore})", string.Join(" ", houseCardsString)); 
            embed.AddField((splitHandPosition == 0 ? "➡ " : "") + $"First hand ({firstHandScore})" + firstHandStateString, string.Join(" ", userFirstHandCardsString));
            embed.AddField((splitHandPosition == 1 ? "➡ " : "") + $"Second hand ({secondHandScore})" + secondHandStateString, string.Join(" ", userSecondHandCardsString));
                
            if (_message is null)
                _message = await _channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
            else
                await _message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
        }

        private async Task AddReactionsAsync()
        {
            await _message.AddReactionAsync(_hit).ConfigureAwait(false);
            await _message.AddReactionAsync(_stand).ConfigureAwait(false);
        }

        private async Task TakeBet()
        {
            using (var db = _service.Db.GetDbContext())
            {
                var user = db.Users.FirstOrDefault(x => x.UserId == _user.Id);
                if (user != null)
                {
                    user.Currency -= _bet;
                    await db.SaveChangesAsync();
                }
            }
        }
        
        private async Task GivePrize(int currency)
        {
            using (var db = _service.Db.GetDbContext())
            {
                var user = db.Users.FirstOrDefault(x => x.UserId == _user.Id);
                if (user != null)
                {
                    user.Currency += currency;
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task StopGameAsync(IGuildUser user)
        {
            if (_bot.GuildPermissions.ManageMessages)
                await _message.RemoveAllReactionsAsync();
            _service.RemoveGame(_user);
        }
        
        private void UserTurn(PlayerHand userHand)
        {
            userHand.Cards.Add(DrawCardFromDeck());
        }

        private void DealerTurn()
        {
            if (_userHand.HandState == PlayerHandState.Higher)
                return;
            
            var score = UpdateHouse();
            if (score < 17)
            {
                _house.Cards.Add(DrawCardFromDeck());
            }
            else if (score <= 21)
            {
                if (score == 17)
                {
                    if (_house.Cards.All(x => x.Number != 1))
                        _house.Status = PlayerStatus.Standing;
                    else
                    {
                        _house.Cards.Add(DrawCardFromDeck());
                    }
                }
                else
                {
                    _house.Status = PlayerStatus.Standing;
                }
            }
            else
            {
                _house.Status = PlayerStatus.Standing;
            }
        }

        private int UpdateUserHand(PlayerHand hand)
        {
            var aces = hand.Cards.Count(x => x.Number == 1);
            var score = 0;

            if (aces > 0)
            {
                var foundAce = false;
                score = 11;
                foreach (var card in hand.Cards)
                {
                    if (!foundAce)
                    {
                        if (card.Number == 1)
                        {
                            foundAce = true;
                            continue;
                        }
                    }
                    if (card.Number > 10)
                        score += 10;
                    else
                        score += card.Number;
                }

                if (score > 21)
                    score -= 10;    //take ace as 1
            }
            else
            {
                foreach (var card in hand.Cards)
                {
                    if (card.Number >= 10)
                        score += 10;
                    else
                        score += card.Number;
                }
            }

            if (score == 21)
            {
                hand.HandState = PlayerHandState.Blackjack;
                hand.Status = PlayerStatus.Standing;
            }
            else
            {
                if (score > 21)
                {
                    hand.HandState = PlayerHandState.Higher;
                    hand.Status = PlayerStatus.Standing;
                }
                else
                {
                    hand.HandState = PlayerHandState.Lower;
                }
            }
            return score;
        }
        
        private int UpdateHouse()
        {
            var aces = _house.Cards.Count(x => x.Number == 1);
            var score = 0;

            if (aces > 0)
            {
                var foundAce = false;
                score = 11;
                foreach (var card in _house.Cards)
                {
                    if (!foundAce)
                    {
                        if (card.Number == 1)
                        {
                            foundAce = true;
                            continue;
                        }
                    }
                    if (card.Number > 10)
                        score += 10;
                    else
                        score += card.Number;
                }

                if (score > 21)
                    score -= 10;    //take ace as 1
            }
            else
            {
                foreach (var card in _house.Cards)
                {
                    if (card.Number >= 10)
                        score += 10;
                    else
                        score += card.Number;
                }
            }

            if (score == 21)
                _house.HandState = PlayerHandState.Blackjack;
            else
                _house.HandState = score > 21 ? PlayerHandState.Higher : PlayerHandState.Lower;
            return score;
        }

        private void SplitUserHand()
        {
            var firstHand = new PlayerHand
            {
                HandState = _userHand.HandState,
                Cards = new List<Card> {_userHand.Cards[0]},
                Status = _userHand.Status
            };
            var secondHand = new PlayerHand
            {
                HandState = _userHand.HandState,
                Cards = new List<Card> {_userHand.Cards[1]},
                Status = _userHand.Status
            };

            _userSplitHands = new List<PlayerHand>
            {
                firstHand,
                secondHand
            };
        }

        private int GetHouseFirstCardNumber()
        {
            var card = _house.Cards.FirstOrDefault();
            if (card != null)
            {
                if (card.Number == 1)
                    return 11;
                if (card.Number > 10)
                    return 10;
                return card.Number;
            }

            return 0;
        }

        private void InitializeDeck()
        {
            _deck = new List<Card>();
            for (var deck = 0; deck < 4; deck++)
            {
                for (var suit = 0; suit < 4; suit++)
                {
                    for (var cardNumber = 0; cardNumber < 13; cardNumber++)
                    {
                        var cardNumberString = (cardNumber + 1).ToString();
                        var cardType = _suits[suit];
                        if (cardNumber == 0)
                        {
                            cardNumberString = "A";
                        }
                        else if (cardNumber >= 10)
                        {
                            cardNumberString = _highCards[cardNumber - 10];
                        }
                    
                        _deck.Add(new Card
                        {
                            Number = cardNumber + 1,
                            NumberString = cardNumberString,
                            Type = cardType
                        });
                    }
                }
            }
            _deck.Shuffle();
        }
        
        private Card DrawCardFromDeck()
        {
            var random = new Random((int) DateTime.UtcNow.Ticks);
            var randomCard = random.Next(_deck.Count);
            var card = _deck[randomCard];
            _deck.Remove(card);
            return card;
        }

        private static IEnumerable<string> CardsString(IEnumerable<Card> cards)
        {
            return cards.Select(x => x.NumberString + x.Type).ToList();
        }

        private enum PlayerStatus
        {
            Playing = 0,
            Standing = 1
        }

        private enum PlayerHandState    //the state of the player's game, lower "< 21", higher "> 21", or blackjack "= 21"
        {
            Lower = 0,
            Higher = 1,
            Blackjack = 2
        }
    }
}