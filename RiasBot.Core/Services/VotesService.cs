using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RiasBot.Services.Database.Models;
using RiasBot.Services.WebSockets;

namespace RiasBot.Services
{
    public class VotesService : IRService
    {
        private readonly IBotCredentials _creds;
        private readonly DbService _db;
        public VotesService(IBotCredentials creds, DbService db)
        {
            _creds = creds;
            _db = db;
        }
        public List<Votes> VotesList;
        private VotesWebSocket _votesWebSocket;

        private string protocol;

        public async Task ConfigureVotesWebSocket()
        {
            if (string.IsNullOrEmpty(_creds.VotesManagerConfig.WebSocketHost) || _creds.VotesManagerConfig.WebSocketPort == 0)
            {
                //the votes manager is not configured
                return;
            }
            
            protocol = _creds.VotesManagerConfig.IsSecureConnection ? "https" : "http";
            
            _votesWebSocket = new VotesWebSocket(_creds.VotesManagerConfig);
            await _votesWebSocket.Connect().ConfigureAwait(false);

            _votesWebSocket.OnConnected += VotesWebSocketConnected;
            _votesWebSocket.OnReceive += AwardVoter;
        }

        private async Task LoadVotes()
        {
            try
            {
                using (var db = _db.GetDbContext())
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("Authorization", _creds.VotesManagerConfig.Authorization);
                    var votesApi = await http.GetStringAsync($"{protocol}://{_creds.VotesManagerConfig.WebSocketHost}/api/votes");
                    var dblVotes = JsonConvert.DeserializeObject<DBL>(votesApi);
                    
                    VotesList = new List<Votes>();
                    var votes = dblVotes.Votes.Where(x => x.Type == "upvote").ToList();
                    
                    foreach (var vote in votes)
                    {
                        var date = vote.Date.AddHours(12);
                        if (DateTime.Compare(date.ToUniversalTime(), DateTime.UtcNow) >= 1)
                        {
                            VotesList.Add(vote);
                            if (vote.IsChecked) continue;
                            
                            var userDb = db.Users.FirstOrDefault(x => x.UserId == vote.User);
                            if (userDb != null)
                            {
                                if (!userDb.IsBlacklisted)
                                    userDb.Currency += vote.IsWeekend ? 50 : 25;
                            }
                            else
                            {
                                var currency = new UserConfig { UserId = vote.User, Currency = vote.IsWeekend ? 50 : 25 };
                                await db.AddAsync(currency);
                            }

                            await UpdateVote(vote.User);
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
                Console.WriteLine($"{DateTime.UtcNow:MMM dd hh:mm:ss} The votes list was loaded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        
        private async Task RefreshVotes()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("Authorization", _creds.VotesManagerConfig.Authorization);
                    var votesApi = await http.GetStringAsync($"{protocol}://{_creds.VotesManagerConfig.WebSocketHost}/api/votes");
                    var dblVotes = JsonConvert.DeserializeObject<DBL>(votesApi);
                    
                    VotesList = new List<Votes>();
                    var votes = dblVotes.Votes.Where(x => x.Type == "upvote").ToList();
                    
                    foreach (var vote in votes)
                    {
                        var date = vote.Date.AddHours(12);
                        if (DateTime.Compare(date.ToUniversalTime(), DateTime.UtcNow) >= 1)
                        {
                            VotesList.Add(vote);
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task AwardVoter(JObject vote)
        {
            using (var db = _db.GetDbContext())
            {
                var userId = ulong.Parse(vote["User"].ToString());
                var isWeekend = (bool) vote["IsWeekend"];
                var userDb = db.Users.FirstOrDefault(x => x.UserId == userId);
                if (userDb != null)
                {
                    if (!userDb.IsBlacklisted)
                        userDb.Currency += isWeekend ? 50 : 25;
                }
                else
                {
                    var currency = new UserConfig { UserId = userId, Currency = isWeekend ? 50 : 25 };
                    await db.AddAsync(currency);
                }
                await UpdateVote(userId);
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task UpdateVote(ulong userId)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("Authorization", _creds.VotesManagerConfig.Authorization);

                    await http.PostAsync($"{protocol}://{_creds.VotesManagerConfig.WebSocketHost}/api/votes/{userId}", null).ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex);
            }
            await RefreshVotes();
        }

        private async Task VotesWebSocketConnected()
        {
            await LoadVotes();
        }
    }
    
    public class DBL
    {
        public List<Votes> Votes { get; set; }
        public DateTime Date { get; set; }
    }
    public class Data
    {
        
    }
    public class Votes
    {
        public ulong Bot { get; set; }
        public ulong User { get; set; }
        public string Type { get; set; }
        public bool IsWeekend { get; set; }
        public string Query { get; set; }
        public DateTime Date { get; set; }
        public bool IsChecked { get; set; }
    }
}