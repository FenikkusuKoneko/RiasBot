using System;
using System.Linq;
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
using PatronStatus = Rias.Core.Models.PatronStatus;

namespace Rias.Core.Services
{
    [AutoStart]
    public class PatreonService : RiasService
    {
        public const int ProfileColorTier = 1;
        public const int ProfileFirstBadgeTier = 2;
        public const int ProfileSecondBadgeTier = 3;
        public const int ProfileThirdBadgeTier = 4;

        private readonly WebSocketClient? _webSocket;

        public PatreonService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            var credentials = serviceProvider.GetRequiredService<Credentials>();
            if (credentials.PatreonConfig != null)
            {
                _webSocket = new WebSocketClient(credentials.PatreonConfig);
                RunTaskAsync(ConnectWebSocket());
                _webSocket.DataReceived += PledgeReceivedAsync;
                _webSocket.Closed += WebSocketClosed;
                
                RunTaskAsync(CheckPatronsAsync());
            }
        }

        private async Task CheckPatronsAsync()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var patrons = await db.Patreon.Where(x => x.PatronStatus == PatronStatus.ActivePatron &&
                                                      !x.Checked && x.Tier > 0)
                .ToListAsync();
            
            foreach (var patron in patrons)
            {
                var reward = patron.AmountCents * 5;
                var userDb = await db.GetOrAddAsync(x => x.UserId == patron.UserId, () => new UsersEntity {UserId = patron.UserId});
                userDb.Currency += reward;

                patron.Checked = true;
                Log.Information($"Patreon discord user with ID {patron.UserId} was rewarded with {reward} hearts");
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
                    Log.Information("Patreon WebSocket connected.");
                    break;
                }
                catch
                {
                    Log.Warning("Patreon WebSocket couldn't connect. Retrying in 10 seconds...");
                    await Task.Delay(10000);
                }
            }
        }
        
        private async Task PledgeReceivedAsync(string data)
        {
            var pledgeData = JsonConvert.DeserializeObject<PatreonPledge>(data);
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var patreonDb = await db.Patreon.FirstOrDefaultAsync(x => x.UserId == pledgeData.DiscordId);
            
            if (patreonDb is null)
            {
                Log.Error($"Couldn't take the patreon data from the database for user {pledgeData.DiscordId}");
                return;
            }
            
            var reward = pledgeData.AmountCents * 5;
            var userDb = await db.GetOrAddAsync(x => x.UserId == pledgeData.DiscordId, () => new UsersEntity {UserId = pledgeData.DiscordId});
            userDb.Currency += reward;
            
            patreonDb.Checked = true;
            await db.SaveChangesAsync();
            
            Log.Information($"Patreon discord user with ID {patreonDb.UserId} was rewarded with {reward} hearts");
        }

        private async Task WebSocketClosed()
        {
            Log.Warning("Patreon WebSocket was closed. Retrying in 10 seconds...");
            await Task.Delay(10000);
            await RunTaskAsync(ConnectWebSocket());
        }
    }
}