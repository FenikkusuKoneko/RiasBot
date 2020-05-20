using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Core.Attributes;
using Rias.Core.Database;
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;
using Rias.Core.Models;
using Rias.Core.Services.WebSocket;
using Serilog;
using Serilog.Events;

namespace Rias.Core.Services
{
    [AutoStart]
    public class VotesService : RiasService
    {
        private readonly HttpClient _httpClient;
        
        private Timer? DblTimer { get; }
        
        public VotesService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            
            var credentials = serviceProvider.GetRequiredService<Credentials>();
            if (credentials.VotesConfig != null)
            {
                var websocket = new RiasWebSocket(credentials.VotesConfig, "VotesWebSocket");
                RunTaskAsync(websocket.ConnectAsync());

                websocket.Log += LogAsync;
                websocket.DataReceived += VoteReceivedAsync;
            }

            if (!string.IsNullOrEmpty(credentials.DiscordBotListToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Credentials.DiscordBotListToken);
                DblTimer = new Timer(async _ => await PostDiscordBotListStats(), null, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30));
            }

            RunTaskAsync(CheckVotesAsync());
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

        private Task LogAsync(MessageLoggedEventArgs args)
        {
            var logEventLevel = args.Severity switch
            {
                LogMessageSeverity.Trace => LogEventLevel.Verbose,
                LogMessageSeverity.Information => LogEventLevel.Information,
                LogMessageSeverity.Debug => LogEventLevel.Debug,
                LogMessageSeverity.Warning => LogEventLevel.Warning,
                LogMessageSeverity.Error => LogEventLevel.Error,
                LogMessageSeverity.Critical => LogEventLevel.Fatal,
                _ => LogEventLevel.Verbose
            };

            Log.Logger.Write(logEventLevel, $"{args.Source}: {args.Message}");

            return Task.CompletedTask;
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