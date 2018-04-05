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
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        private List<PlayerNumbers> playerNumbers;

        private IMessageChannel channel;
        private int reward;
        private int maximum;
        private int differencePerUser;
        private bool onlyNumbers;
        private bool botStarts;

        private int numberCounter;
        public bool gameStarted;

        public EventService(DiscordSocketClient client, DbService db)
        {
            _client = client;
            _db = db;

            _client.MessageReceived += onMessageReceived;
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

        public async Task onMessageReceived(SocketMessage s)
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
                                string[] players = new string[playerNumbers.Count];
                                playerNumbers = playerNumbers.GroupBy(x => x.User.ToString()).Select(y => y.FirstOrDefault()).OrderBy(y => y.User.ToString()).ToList();
                                for (int i = 0; i < playerNumbers.Count; i++)
                                {
                                    players[i] = playerNumbers[i].User.ToString();
                                }
                                await channel.SendConfirmationEmbed($"Congratulations {Format.Bold(String.Join(" ", players).Trim())} you earned {reward}{RiasBot.currency}!");
                                await AwardUsers().ConfigureAwait(false);
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
                        await AwardUsers().ConfigureAwait(false);
                    }
                }
                else
                {
                    await message.DeleteAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task AwardUsers()
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
    }

    public class PlayerNumbers
    {
        public IGuildUser User { get; set; }
        public int Number { get; set; }
    }
}
