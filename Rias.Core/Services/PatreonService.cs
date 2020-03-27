using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
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
    public class PatreonService : RiasService
    {
        public const int ProfileColorTier = 1;
        public const int ProfileFirstBadgeTier = 2;
        public const int ProfileSecondBadgeTier = 3;
        public const int ProfileThirdBadgeTier = 4;

        public PatreonService(IServiceProvider services) : base(services)
        {
            var creds = services.GetRequiredService<Credentials>();
            if (creds.PatreonConfig != null)
            {
                var websocket = new RiasWebSocket(creds.PatreonConfig, "PatreonWebSocket");
                RunTaskAsync(websocket.ConnectAsync());
                websocket.Log += LogAsync;
                websocket.DataReceived += PledgeReceivedAsync;
            }
        }

        public async Task CheckPatronsAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var patrons = db.Patreon.Where(x => x.PatronStatus == PatronStatus.ActivePatron &&
                                                !x.Checked && x.Tier > 0);
            
            foreach (var patron in patrons)
            {
                var reward = patron.AmountCents * 5;
                var userDb = await db.GetOrAddAsync(x => x.UserId == patron.UserId, () => new Users {UserId = patron.UserId});
                userDb.Currency += reward;

                patron.Checked = true;
                Log.Information($"Patreon discord user with ID {patron.UserId} was rewarded with {reward} hearts");
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

        private async Task PledgeReceivedAsync(string data)
        {
            var pledgeData = JsonConvert.DeserializeObject<PatreonPledgeData>(data);
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var patreonDb = await db.Patreon.FirstOrDefaultAsync(x => x.UserId == pledgeData.DiscordId);
            
            if (patreonDb is null)
            {
                Log.Error($"Couldn't take the patreon data from the database for user {pledgeData.DiscordId}");
                return;
            }
            
            var reward = pledgeData.AmountCents * 5;
            var userDb = await db.GetOrAddAsync(x => x.UserId == pledgeData.DiscordId, () => new Users {UserId = pledgeData.DiscordId});
            userDb.Currency += reward;
            
            patreonDb.Checked = true;
            await db.SaveChangesAsync();
            
            Log.Information($"Patreon discord user with ID {patreonDb.UserId} was rewarded with {reward} hearts");
        }
    }
}