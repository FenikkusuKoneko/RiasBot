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
        private readonly DiscordShardedClient _client;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        private Timer timer;

        public PatreonService(DiscordShardedClient client, DbService db, IBotCredentials creds)
        {
            _client = client;
            _db = db;
            _creds = creds;

            if (!RiasBot.IsBeta && !String.IsNullOrEmpty(_creds.PatreonAccessToken))
            {
                timer = new Timer(new TimerCallback(async _ => await RewardPatron()), null, TimeSpan.Zero, new TimeSpan(1, 0, 0));
            }
        }
        private int campaignId;

        public async Task Patreon()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _creds.PatreonAccessToken);

                var url = "https://www.patreon.com/api/oauth2/api/current_user/campaigns";
                var data = await http.GetAsync(url);
                if (data.IsSuccessStatusCode)
                {
                    try
                    {
                        var patreonCurrentUser = JsonConvert.DeserializeObject<PatreonCurrentUser>(await data.Content.ReadAsStringAsync());
                        campaignId = patreonCurrentUser.Included.FirstOrDefault().Relationships.Campaign.Data.Id;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        public async Task<PatreonCampaign> Campaign()
        {
            if (campaignId != 0)
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _creds.PatreonAccessToken);

                    var url = $"https://www.patreon.com/api/oauth2/api/campaigns/{campaignId}/pledges";
                    var data = await http.GetAsync(url);
                    if (data.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<PatreonCampaign>(await data.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public async Task RewardPatron()
        {
            if (campaignId == 0)
            {
                await Patreon();
            }
            var patreonCampaign = await Campaign();
            if (patreonCampaign == null)
                return;

            using (var db = _db.GetDbContext())
            {
                var pledges = patreonCampaign.Data?.Where(x => x.type == "pledge").ToList();
                var patrons = patreonCampaign.Included?.Where(x => x.type == "user").ToList();
                var patronsList = new List<ulong>();

                if (pledges is null || patrons is null)
                {
                    return;
                }
                foreach (var pledge in pledges)
                {
                    var patronPledgeId = pledge.relationships.patron.data.id;
                    var patronUser = patrons.FirstOrDefault(x => x.id == patronPledgeId);

                    if (patronUser != null)
                    {
                        if (pledge.attributes.declined_since != null)
                        {
                            if (DateTime.Compare((DateTime)pledge.attributes.declined_since, DateTime.UtcNow) < 1)
                            {
                                continue;
                            }
                        }
                        var amountCents = pledge.attributes.amount_cents;
                        if (!UInt64.TryParse(patronUser.attributes.social_connections?.discord?.user_id, out var userId))
                        {
                            continue;
                        }
                            

                        if (userId > 0)
                        {
                            patronsList.Add(userId);
                            var patronDb = db.Patreon.FirstOrDefault(x => x.UserId == userId);
                            var userDb = db.Users.FirstOrDefault(x => x.UserId == userId);
                            if (patronDb != null)
                            {
                                patronDb.Reward = amountCents * 10;

                                var lastTimeAwarded = patronDb.NextTimeReward;

                                var lastTimeAwardedValid = DateTime.Compare(lastTimeAwarded, DateTime.UtcNow);
                                if (lastTimeAwardedValid <= 0)
                                {
                                    if (userDb != null)
                                    {
                                        userDb.Currency += amountCents * 10;
                                    }
                                    else
                                    {
                                        var user = new UserConfig { UserId = userId, Currency = amountCents * 10 };
                                        await db.AddAsync(user).ConfigureAwait(false);
                                    }
                                    var nextTimeAward = lastTimeAwarded.AddMonths(1);
                                    patronDb.NextTimeReward = new DateTime(nextTimeAward.Year, nextTimeAward.Month, 3);
                                    
                                    try
                                    {
                                        var user = (IUser)_client.GetUser(userId);

                                        var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                                        embed.WithTitle("Patreon Support!");
                                        embed.WithDescription("Thank you so much for supporting the project :heart:.");
                                        embed.AddField("Pledge", amountCents / 100 + "$", true).AddField("Reward", amountCents * 10 + RiasBot.Currency);
                                        await user.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        //user not found
                                    }
                                }
                            }
                            else
                            {
                                var nextTimeAward = DateTime.UtcNow.AddMonths(1);
                                var patron = new Patreon { UserId = userId, Reward = amountCents * 10, NextTimeReward = new DateTime(nextTimeAward.Year, nextTimeAward.Month, 3) };
                                await db.AddAsync(patron).ConfigureAwait(false);
                            }
                        }
                    }
                }
                await db.SaveChangesAsync().ConfigureAwait(false);

                foreach (var dbPatron in db.Patreon)
                {
                    if (!patronsList.Contains(dbPatron.UserId))
                    {
                        db.Remove(dbPatron);
                    }
                }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
