using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Core.Attributes;
using Rias.Core.Database;
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;
using Rias.Core.Models;
using Serilog;

namespace Rias.Core.Services
{
    [AutoStart]
    public class VotesService : RiasService
    {
        private readonly HttpClient _httpClient;
        private readonly WebSocketClient? _webSocket;
        private Timer? DblTimer { get; }
        
        public VotesService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            
            var credentials = serviceProvider.GetRequiredService<Credentials>();
            if (credentials.VotesConfig != null)
            {
                _webSocket = new WebSocketClient(credentials.VotesConfig);
                RunTaskAsync(ConnectWebSocket());
                _webSocket.DataReceived += VoteReceivedAsync;
                _webSocket.Closed += WebSocketClosed;
                
                RunTaskAsync(CheckVotesAsync());
            }

            if (!string.IsNullOrEmpty(credentials.DiscordBotListToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Credentials.DiscordBotListToken);
                DblTimer = new Timer(async _ => await PostDiscordBotListStats(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
        }
        
        private async Task CheckVotesAsync()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var votes = await db.Votes.Where(x => !x.Checked).ToListAsync();
            foreach (var vote in votes)
            {
                var userDb = await db.GetOrAddAsync(x => x.UserId == vote.UserId, () => new UsersEntity {UserId = vote.UserId});
                if (userDb.IsBlacklisted)
                {
                    db.Remove(vote);
                    Log.Information($"Vote discord user with ID {vote.UserId} is blacklisted, it was removed from the votes database table");
                    continue;
                }

                var reward = vote.IsWeekend ? 50 : 25;
                userDb.Currency += reward;
                
                vote.Checked = true;
                Log.Information($"Vote discord user with ID {vote.UserId} was rewarded with {reward} hearts");
            }
            
            await db.SaveChangesAsync();
        }

        private async Task ConnectWebSocket()
        {
            while (true)
            {
                try
                {
                    await _webSocket!.ConnectAsync();
                    Log.Information("Votes WebSocket connected.");
                    break;
                }
                catch
                {
                    Log.Warning("Votes WebSocket couldn't connect. Retrying in 10 seconds...");
                    await Task.Delay(10000);
                }
            }
        }
        
        private async Task VoteReceivedAsync(string data)
        {
            var voteData = JsonConvert.DeserializeObject<Vote>(data);
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var voteDb = await db.Votes.FirstOrDefaultAsync(x => x.UserId == voteData.UserId && !x.Checked);
            
            if (voteDb is null)
            {
                Log.Error($"Couldn't take the vote data from the database for user {voteData.UserId}");
                return;
            }

            var reward = voteDb.IsWeekend ? 50 : 25;
            var userDb = await db.GetOrAddAsync(x => x.UserId == voteData.UserId, () => new UsersEntity {UserId = voteData.UserId});
            userDb.Currency += reward;
            
            voteDb.Checked = true;
            await db.SaveChangesAsync();
            
            Log.Information($"Vote discord user with ID {voteDb.UserId} was rewarded with {reward} hearts");
        }
        
        private async Task WebSocketClosed()
        {
            Log.Warning("Votes WebSocket was closed. Retrying in 10 seconds...");
            await Task.Delay(10000);
            await RunTaskAsync(ConnectWebSocket());
        }
        
        private async Task PostDiscordBotListStats()
        {
            if (RiasBot.CurrentUser is null)
                return;
            
            try
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"shard_count", RiasBot.Shards.Count.ToString()},
                    {"server_count", RiasBot.Guilds.Count.ToString()}
                });
                await _httpClient.PostAsync($"https://top.gg/api/bots/{RiasBot.CurrentUser.Id}/stats", content);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}