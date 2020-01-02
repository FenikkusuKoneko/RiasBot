using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Implementation;
using Rias.Core.Services.Websocket;
using Serilog;
using Serilog.Events;

namespace Rias.Core.Services
{
    public class VotesService : RiasService
    {
        private readonly DiscordShardedClient _client;
        private readonly GamblingService _gamblingService;
        private readonly HttpClient _httpClient;

        private Timer? DblTimer { get; }
        private const int DatabaseVoteAttempts = 5;
        
        public VotesService(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _gamblingService = services.GetRequiredService<GamblingService>();
            _httpClient = new HttpClient();
            
            var creds = services.GetRequiredService<Credentials>();
            if (creds.VotesConfig != null)
            {
                var websocket = new RiasWebsocket();
                websocket.ConnectAsync(creds.VotesConfig);

                websocket.OnConnected += ConnectedAsync;
                websocket.OnDisconnected += DisconnectedAsync;
                websocket.Log += LogAsync;
                websocket.OnReceive += VoteReceivedAsync;
            }

            if (!string.IsNullOrEmpty(creds.DiscordBotListApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", creds.DiscordBotListApiKey);
                DblTimer = new Timer(async _ => await PostDiscordBotListStats(), null, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30));
            }
        }
        
        public async Task CheckVotesAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var votes = db.Votes.Where(x => !x.Checked);
            foreach (var vote in votes)
            {
                var userDb = await db.Users.FirstOrDefaultAsync(x => x.UserId == vote.UserId);
                if (userDb != null && userDb.IsBlacklisted)
                {
                    db.Remove(vote);
                    Log.Information($"Vote discord user with ID {vote.UserId} is blacklisted, it was removed from the votes database table");
                    continue;
                }

                var reward = vote.IsWeekend ? 50 : 25;
                await _gamblingService.AddUserCurrencyAsync(vote.UserId, reward);
                
                vote.Checked = true;
                Log.Information($"Vote discord user with ID {vote.UserId} was rewarded with {reward} hearts");
            }
            
            await db.SaveChangesAsync();
        }

        private Task ConnectedAsync()
        {
            Log.Information("Votes websocket connected");
            return Task.CompletedTask;
        }
        
        private Task DisconnectedAsync(WebSocketCloseStatus? status, string description)
        {
            Log.Information($"Votes websocket diconnected. Status: {status}. Description: {description}");
            return Task.CompletedTask;
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

            Log.Logger.Write(logEventLevel, $"Votes websocket: {msg.Message}");

            return Task.CompletedTask;
        }
        
        private async Task VoteReceivedAsync(string data)
        {
            var voteData = JsonConvert.DeserializeObject<VoteData>(data);
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var voteDb = await db.Votes.FirstOrDefaultAsync(x => x.UserId == voteData.User && !x.Checked);

            var attempts = 0;
            while (voteDb is null && attempts < DatabaseVoteAttempts)
            {
                await Task.Delay(5000);
                
                voteDb = await db.Votes.FirstOrDefaultAsync(x => x.UserId == voteData.User && !x.Checked);
                attempts++;
            }

            if (voteDb is null)
            {
                Log.Error($"Couldn't take the vote data from the database for user {voteData.User}");
                return;
            }

            var reward = voteDb.IsWeekend ? 50 : 25;
            await _gamblingService.AddUserCurrencyAsync(voteDb.UserId, reward);

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
                using var content = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        {"shard_count", _client.Shards.Count.ToString()},
                        //{ "shard_id", _discord.ShardId.ToString() },
                        {"server_count", _client.Guilds.Count.ToString()}
                    });
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                await _httpClient.PostAsync($"{Creds.DiscordBotList}/stats", content);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}