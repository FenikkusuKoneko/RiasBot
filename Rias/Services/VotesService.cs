using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rias.Attributes;
using Rias.Database;
using Rias.Database.Entities;
using Serilog;

namespace Rias.Services
{
    [AutoStart]
    public class VotesService : RiasService
    {
        private readonly WebSocketClient? _webSocket;
        
        public VotesService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            if (Configuration.VotesConfig == null) return;
            
            _webSocket = new WebSocketClient(Configuration.VotesConfig);
            RunTaskAsync(ConnectWebSocket());
            _webSocket.DataReceived += VoteReceivedAsync;
            _webSocket.Closed += WebSocketClosed;
                
            RunTaskAsync(CheckVotesAsync);
        }

        private async Task CheckVotesAsync()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var votes = await db.Votes.Where(x => !x.Checked).ToListAsync();
            foreach (var vote in votes)
            {
                var userDb = await db.GetOrAddAsync(x => x.UserId == vote.UserId, () => new UsersEntity { UserId = vote.UserId });
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

        private async Task ConnectWebSocket(bool recheckVotes = false)
        {
            while (true)
            {
                try
                {
                    await _webSocket!.ConnectAsync();
                    Log.Information("Votes WebSocket connected.");

                    if (recheckVotes)
                        await RunTaskAsync(CheckVotesAsync);
                    
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
            try
            {
                var voteData = JsonConvert.DeserializeObject<JToken>(data);
                var userId = voteData.Value<ulong>("user");
                
                using var scope = RiasBot.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
                var voteDb = await db.Votes.FirstOrDefaultAsync(x => x.UserId == userId && !x.Checked);

                if (voteDb is null)
                {
                    Log.Error($"Couldn't take the vote data from the database for user {userId}");
                    return;
                }

                var reward = voteDb.IsWeekend ? 50 : 25;
                var userDb = await db.GetOrAddAsync(x => x.UserId == userId, () => new UsersEntity { UserId = userId });
                userDb.Currency += reward;

                voteDb.Checked = true;
                await db.SaveChangesAsync();

                Log.Information($"Vote discord user with ID {userId} was rewarded with {reward} hearts");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on receiving a vote");
            }
        }
        
        private async Task WebSocketClosed()
        {
            Log.Warning("Votes WebSocket was closed. Retrying in 10 seconds...");
            await Task.Delay(10000);
            await RunTaskAsync(ConnectWebSocket(true));
        }
    }
}