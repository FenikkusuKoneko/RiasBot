using Discord;
using Discord.WebSocket;
using RiasBot.Commons.Patreon;
using RiasBot.Services.Database.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Services
{
    public class PatreonService : IRService
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        private Timer timer;

        public PatreonService(DiscordSocketClient client, DbService db, IBotCredentials creds)
        {
            _client = client;
            _db = db;
            _creds = creds;

            if (!RiasBot.isBeta && !String.IsNullOrEmpty(_creds.PatreonAccessToken))
            {
                var patreon = Task.Run(async () => await Patreon());
                Task.WaitAll(patreon);
                timer = new Timer(new TimerCallback(async _ => await RewardPatron()), null, TimeSpan.Zero, new TimeSpan(1, 0, 0));
            }
        }
        public int campaignId;

        public async Task Patreon()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _creds.PatreonAccessToken);

                var url = "https://www.patreon.com/api/oauth2/api/current_user/campaigns";
                var data = await http.GetStringAsync(url);

                var patreonCurrentUser = JsonConvert.DeserializeObject<PatreonCurrentUser>(data);
                campaignId = patreonCurrentUser.Included.FirstOrDefault().Relationships.Campaign.Data.Id;
            }
        }

        public async Task<PatreonCampaign> Campaign()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _creds.PatreonAccessToken);

                var url = $"https://www.patreon.com/api/oauth2/api/campaigns/{campaignId}/pledges";
                var data = await http.GetStringAsync(url);

                return JsonConvert.DeserializeObject<PatreonCampaign>(data);
            }
        }

        public async Task RewardPatron()
        {
            var patreonCampaign = await Campaign();
            using (var db = _db.GetDbContext())
            {
                var pledges = patreonCampaign.Data?.Where(x => x.type == "pledge").ToList();
                var patrons = patreonCampaign.Included?.Where(x => x.type == "user").ToList();
                var validPledges = db.Patreon.ToList();
                var patronsList = new List<ulong>();

                if (pledges is null || patrons is null)
                {
                    return;
                }

                int index = 0;
                foreach (var pledge in pledges)
                {
                    int patronPledgeId = pledge.relationships.patron.data.id;
                    int patronId = patrons[index].id;

                    if (patronPledgeId == patronId)
                    {
                        int amountCents = pledge.attributes.amount_cents;
                        UInt64.TryParse(patrons[index].attributes.social_connections.discord.user_id, out var userId);

                        if (userId > 0)
                        {
                            patronsList.Add(userId);

                            var patronDb = db.Patreon.Where(x => x.UserId == userId).FirstOrDefault();
                            var userDb = db.Users.Where(x => x.UserId == userId).FirstOrDefault();
                            try
                            {
                                patronDb.Reward = amountCents * 10;

                                var lastTimeAwarded = patronDb.NextTimeReward;

                                var lastTimeAwardedValid = DateTime.Compare(lastTimeAwarded, DateTime.UtcNow);
                                if (lastTimeAwardedValid <= 0)
                                {
                                    try
                                    {
                                        userDb.Currency += amountCents * 10;
                                    }
                                    catch
                                    {
                                        var user = new UserConfig { UserId = userId, Currency = amountCents * 10 };
                                        await db.AddAsync(user).ConfigureAwait(false);
                                    }
                                    var nextTimeAward = lastTimeAwarded.AddMonths(1);
                                    patronDb.NextTimeReward = new DateTime(nextTimeAward.Year, nextTimeAward.Month, 1, 8, 0, 0);
                                    try
                                    {
                                        var user = (IUser)_client.GetUser(userId);

                                        var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                                        embed.WithTitle("Patreon Support!");
                                        embed.WithDescription("Thank you so much for supporting the project :heart:");
                                        embed.AddField("Pledge", amountCents + "$", true).AddField("Reward", amountCents * 10 + RiasBot.currency);
                                        await user.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        //user not found
                                    }
                                }
                            }
                            catch
                            {
                                var nextTimeAward = DateTime.UtcNow.AddMonths(1);
                                var patron = new Patreon { UserId = userId, Reward = amountCents * 10, NextTimeReward = new DateTime(nextTimeAward.Year, nextTimeAward.Month, 1, 8, 0, 0) };
                                await db.AddAsync(patron).ConfigureAwait(false);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    index++;
                }
                foreach(var patronId in patronsList)
                {
                    if (validPledges.Any(x => x.UserId != patronId))
                    {
                        db.Remove(db.Patreon.Where(x => x.UserId != patronId).FirstOrDefault());
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
