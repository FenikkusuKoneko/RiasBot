using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Implementation;
using Rias.Core.Services.WebSocket;
using Serilog;
using Serilog.Events;

namespace Rias.Core.Services
{
    public class PatreonService : RiasService
    {
        private readonly GamblingService _gamblingService;

        private const int DatabasePledgeAttempts = 5;

        public PatreonService(IServiceProvider services) : base(services)
        {
            _gamblingService = services.GetRequiredService<GamblingService>();
            
            var creds = services.GetRequiredService<Credentials>();
            // if (creds.PatreonConfig != null)
            // {
            //     var websocket = new RiasWebsocket();
            //     websocket.ConnectAsync(creds.PatreonConfig);
            //
            //     websocket.OnConnected += ConnectedAsync;
            //     websocket.OnDisconnected += DisconnectedAsync;
            //     websocket.Log += LogAsync;
            //     websocket.OnReceive += PledgeReceivedAsync;
            // }
        }

        public async Task CheckPatronsAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var patrons = db.Patreon.Where(x => !x.Checked && x.PatronStatus == PatronStatus.ActivePatron);
            foreach (var patron in patrons)
            {
                var reward = patron.AmountCents * 5;
                await _gamblingService.AddUserCurrencyAsync(patron.UserId, reward);

                patron.Checked = true;
                Log.Information($"Patreon discord user with ID {patron.UserId} was rewarded with {reward} hearts");
            }
            
            await db.SaveChangesAsync();
        }

        private Task ConnectedAsync()
        {
            Log.Information("Patreon websocket connected");
            return Task.CompletedTask;
        }
        
        private Task DisconnectedAsync(WebSocketCloseStatus? status, string description)
        {
            Log.Information($"Patreon websocket diconnected. Status: {status}. Description: {description}");
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

            Log.Logger.Write(logEventLevel, $"Patreon websocket: {msg.Message}");

            return Task.CompletedTask;
        }

        private async Task PledgeReceivedAsync(string data)
        {
            var pledgeData = JsonConvert.DeserializeObject<PatreonPledgeData>(data);
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var pledgeDb = await db.Patreon.FirstOrDefaultAsync(x => x.PatreonUserId == pledgeData.PatreonUserId);

            var attempts = 0;
            while (pledgeDb is null && attempts < DatabasePledgeAttempts)
            {
                await Task.Delay(5000);
                
                pledgeDb = await db.Patreon.FirstOrDefaultAsync(x => x.PatreonUserId == pledgeData.PatreonUserId);
                attempts++;
            }

            if (pledgeDb is null)
            {
                Log.Error($"Couldn't take the pledge data from the database for Patreon member: {pledgeData.PatreonUserName}, ID: {pledgeData.PatreonUserId}");
                return;
            }

            var reward = pledgeDb.AmountCents * 5;
            await _gamblingService.AddUserCurrencyAsync(pledgeDb.UserId, reward);

            pledgeDb.Checked = true;
            await db.SaveChangesAsync();
            
            Log.Information($"Patreon discord user with ID {pledgeDb.UserId} was rewarded with {reward} hearts");
        }
    }
}