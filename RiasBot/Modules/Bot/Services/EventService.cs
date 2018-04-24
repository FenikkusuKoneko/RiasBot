using Discord;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Bot.Services
{
    public class EventService : IRService
    {
        private readonly DiscordShardedClient _client;
        private readonly DbService _db;

        //Counter event
        private List<PlayerNumbers> playerNumbers;
        private Queue<IUser> heartUsers;

        private IMessageChannel channel;
        private int reward;
        private int maximum;
        private int differencePerUser;
        private bool onlyNumbers;
        private bool botStarts;

        private int numberCounter;
        public bool gameStarted;

        public EventService(DiscordShardedClient client, DbService db)
        {
            _client = client;
            _db = db;

            _client.MessageReceived += OnMessageReceived;
        }

        public async Task CounterSetup(IMessageChannel channel, int reward, int maximum, int differencePerUser, bool onlyNumbers, bool botStarts)
        {
            this.channel = channel;
            this.reward = reward;
            this.maximum = maximum;
            this.differencePerUser = differencePerUser;
            this.onlyNumbers = onlyNumbers;
            this.botStarts = botStarts;
            numberCounter = 0;
            gameStarted = true;

            playerNumbers = new List<PlayerNumbers>();
            if (botStarts)
            {
                await channel.SendMessageAsync("1").ConfigureAwait(false);
                numberCounter++;
            }
        }

        public async Task OnMessageReceived(SocketMessage s)
        {
            if (gameStarted)
            {
                var msg = s as SocketUserMessage;     // Ensure the message is from a user/bot
                if (msg == null) return;
                if (msg.Author.Id == _client.CurrentUser.Id) return;     // Ignore self when checking commands
                if (msg.Author.IsBot) return;       // Ignore other bots

                if (msg.Channel == channel)
                {
                    if (Int32.TryParse(msg.Content, out int number))
                    {
                        await NumbersGame((IGuildUser)msg.Author, msg, number).ConfigureAwait(false);
                    }
                    else if (onlyNumbers)
                    {
                        await msg.DeleteAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task NumbersGame(IGuildUser user, IUserMessage message, int number)
        {
            var player = playerNumbers.Where(x => x.User == user).LastOrDefault();
            if (player != null)
            {
                if (number == numberCounter + 1)
                {
                    if (playerNumbers.LastOrDefault() != player)
                    {
                        if (number - player.Number >= differencePerUser)
                        {
                            numberCounter++;
                            var newPlayer = new PlayerNumbers { User = user, Number = number };
                            playerNumbers.Add(newPlayer);

                            await message.AddReactionAsync(Emote.Parse(RiasBot.currency)).ConfigureAwait(false);

                            if (number == maximum)
                            {
                                gameStarted = false;
                                await channel.SendConfirmationEmbed($"Congratulations you earned {reward}{RiasBot.currency}!");
                                await AwardUsersCounter().ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await message.DeleteAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await message.DeleteAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    await message.DeleteAsync().ConfigureAwait(false);
                }
            }
            else
            {
                if (number == numberCounter + 1)
                {
                    numberCounter++;
                    var newPlayer = new PlayerNumbers { User = user, Number = number };
                    playerNumbers.Add(newPlayer);

                    await message.AddReactionAsync(Emote.Parse(RiasBot.currency)).ConfigureAwait(false);

                    if (number == maximum)
                    {
                        string[] players = new string[playerNumbers.Count];
                        playerNumbers = playerNumbers.OrderBy(x => x.Number).ToList();
                        for (int i = 0; i < playerNumbers.Count; i++)
                        {
                            players[i] = playerNumbers[i].User.ToString();
                        }
                        await channel.SendConfirmationEmbed($"Congratulations {String.Join(" ", players)} you earned {reward}{RiasBot.currency}!");
                        await AwardUsersCounter().ConfigureAwait(false);
                    }
                }
                else
                {
                    await message.DeleteAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task AwardUsersCounter()
        {
            using (var db = _db.GetDbContext())
            {
                playerNumbers = playerNumbers.GroupBy(x => x.User.ToString()).Select(y => y.FirstOrDefault()).OrderBy(y => y.User.ToString()).ToList();
                foreach (var player in playerNumbers)
                {
                    var userDb = db.Users.Where(x => x.UserId == player.User.Id).FirstOrDefault();
                    if (userDb != null)
                    {
                        userDb.Currency += reward;
                    }
                    else
                    {
                        var userConfig = new UserConfig { UserId = player.User.Id, Currency = reward };
                        await db.AddAsync(userConfig).ConfigureAwait(false);
                    }
                }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task Hearts(IUserMessage message, int timeout, int reward)
        {
            gameStarted = true;
            heartUsers = new Queue<IUser>();
            IEmote heart = Emote.Parse(RiasBot.currency);
            await message.AddReactionAsync(heart).ConfigureAwait(false);

            Action<SocketReaction> heartReaction = async r =>
            {
                try
                {
                    if (r.Emote.Name == heart.Name)
                    {
                        if (!heartUsers.Any(x => x == r.User.Value))
                        {
                            if (r.User.Value != _client.CurrentUser)
                            {
                                heartUsers.Enqueue(r.User.Value);
                                await AwardUsersHearts(r.User.Value, reward).ConfigureAwait(false);
                            }
                        }
                    }
                    if (!gameStarted)
                    {
                        await message.DeleteAsync().ConfigureAwait(false);
                        return;
                    }
                }
                catch
                {
                    //ignored
                }
            };

            using (message.OnReaction(_client, heartReaction))
            {
                await Task.Delay(timeout * 1000).ConfigureAwait(false);
            }
            gameStarted = false;
            await message.DeleteAsync().ConfigureAwait(false);
        }

        public async Task AwardUsersHearts(IUser user, int reward)
        {
            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                if (userDb != null)
                {
                    userDb.Currency += reward;
                }
                else
                {
                    var userConfig = new UserConfig { UserId = user.Id, Currency = reward };
                    await db.AddAsync(userConfig).ConfigureAwait(false);
                }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }

    public class PlayerNumbers
    {
        public IGuildUser User { get; set; }
        public int Number { get; set; }
    }
}
