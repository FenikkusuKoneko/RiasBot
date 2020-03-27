using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Database.Models;
using Rias.Core.Implementation;
using Rias.Core.Services.WebSocket;
using Serilog;
using Serilog.Events;

namespace Rias.Core.Services
{
    public class VotesService : RiasService
    {
        private readonly DiscordShardedClient _client;
        private readonly HttpClient _httpClient;

        private Timer? DblTimer { get; }
        
        public VotesService(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _httpClient = new HttpClient();
            
            var creds = services.GetRequiredService<Credentials>();
            if (creds.VotesConfig != null)
            {
                var websocket = new RiasWebSocket(creds.VotesConfig, "VotesWebSocket");
                RunTaskAsync(websocket.ConnectAsync());

                websocket.Log += LogAsync;
                websocket.DataReceived += VoteReceivedAsync;
            }

            if (!string.IsNullOrEmpty(creds.DiscordBotListToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Creds.DiscordBotListToken);
                DblTimer = new Timer(async _ => await PostDiscordBotListStats(), null, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30));
            }
        }
        
        public async Task CheckVotesAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var votes = await db.Votes.Where(x => !x.Checked).ToListAsync();
            foreach (var vote in votes)
            {
                var userDb = await db.GetOrAddAsync(x => x.UserId == vote.UserId, () => new Users {UserId = vote.UserId});
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

        private Task LogAsync(LogMessage msg)
        {
            var logEventLevel = msg.Severity switch
            {
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Debug => LogEventLevel.Debug,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Critical => LogEventLevel.Fatal,
                _ => LogEventLevel.Verbose
            };

            Log.Logger.Write(logEventLevel, $"{msg.Source}: {msg.Message}");

            return Task.CompletedTask;
        }
        
        private async Task VoteReceivedAsync(string data)
        {
            var voteData = JsonConvert.DeserializeObject<VoteData>(data);
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var voteDb = await db.Votes.FirstOrDefaultAsync(x => x.UserId == voteData.UserId && !x.Checked);
            
            if (voteDb is null)
            {
                Log.Error($"Couldn't take the vote data from the database for user {voteData.UserId}");
                return;
            }

            var reward = voteDb.IsWeekend ? 50 : 25;
            var userDb = await db.GetOrAddAsync(x => x.UserId == voteData.UserId, () => new Users {UserId = voteData.UserId});
            userDb.Currency += reward;
            
            voteDb.Checked = true;
            await db.SaveChangesAsync();
            
            Log.Information($"Vote discord user with ID {voteDb.UserId} was rewarded with {reward} hearts");
        }

        private async Task PostDiscordBotListStats()
        {
            if (_client.CurrentUser is null)
                return;
            
            try
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"shard_count", _client.Shards.Count.ToString()},
                    {"server_count", _client.Guilds.Count.ToString()}
                });
                await _httpClient.PostAsync($"https://top.gg/api/bots/{_client.CurrentUser.Id}/stats", content);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}