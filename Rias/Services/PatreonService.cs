using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rias.Attributes;
using Rias.Database;
using Rias.Database.Entities;
using Rias.Models;
using Serilog;

namespace Rias.Services
{
    [AutoStart]
    public class PatreonService : RiasService
    {
        public const int ProfileColorTier = 1;
        public const int ProfileFirstBadgeTier = 2;
        public const int ProfileSecondBadgeTier = 3;
        public const int ProfileThirdBadgeTier = 4;

        private readonly WebSocketClient? _webSocket;
        private readonly HttpClient? _httpClient;
        private readonly Timer? _sendPatronsTimer;

        public PatreonService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            if (Configuration.PatreonConfiguration != null)
            {
                _webSocket = new WebSocketClient(Configuration.PatreonConfiguration);
                RunTaskAsync(ConnectWebSocket());
                _webSocket.DataReceived += PledgeReceivedAsync;
                _webSocket.Closed += WebSocketClosed;
                
                RunTaskAsync(CheckPatronsAsync);
                
                _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Configuration.PatreonConfiguration.Authorization!);
                _sendPatronsTimer = new Timer(_ => RunTaskAsync(SendPatronsAsync), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }
        }

        private async Task CheckPatronsAsync()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var patrons = await db.Patreon.Where(x => x.PatronStatus == PatronStatus.ActivePatron && !x.Checked && x.Tier > 0)
                .ToListAsync();
            
            foreach (var patron in patrons)
            {
                var reward = patron.AmountCents * 5;
                var userDb = await db.GetOrAddAsync(x => x.UserId == patron.UserId, () => new UsersEntity { UserId = patron.UserId });
                userDb.Currency += reward;

                patron.Checked = true;
                Log.Information($"Patreon discord user with ID {patron.UserId} was rewarded with {reward} hearts");
            }
            
            await db.SaveChangesAsync();
        }

        private async Task ConnectWebSocket(bool recheckPatrons = false)
        {
            while (true)
            {
                try
                {
                    await _webSocket!.ConnectAsync();
                    Log.Information("Patreon WebSocket connected.");
                    
                    if (recheckPatrons)
                        await RunTaskAsync(CheckPatronsAsync);
                    
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
            try
            {
                var pledgeData = JsonConvert.DeserializeObject<JToken>(data);
                var userId = pledgeData.Value<ulong>("discord_id");
                
                using var scope = RiasBot.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
                var patreonDb = await db.Patreon.FirstOrDefaultAsync(x => x.UserId == userId);

                if (patreonDb is null)
                {
                    Log.Error($"Couldn't take the patreon data from the database for user {userId}");
                    return;
                }

                var reward = patreonDb.AmountCents * 5;
                var userDb = await db.GetOrAddAsync(x => x.UserId == userId, () => new UsersEntity { UserId = userId });
                userDb.Currency += reward;

                patreonDb.Checked = true;
                await db.SaveChangesAsync();

                Log.Information($"Patreon discord user with ID {userId} was rewarded with {reward} hearts");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on receiving a pledge");
            }
        }

        private async Task WebSocketClosed()
        {
            Log.Warning("Patreon WebSocket was closed. Retrying in 10 seconds...");
            await Task.Delay(10000);
            await RunTaskAsync(ConnectWebSocket(true));
        }

        private async Task SendPatronsAsync()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var patrons = (await db.GetOrderedListAsync<PatreonEntity, int>(
                    x => x.PatronStatus == PatronStatus.ActivePatron && x.Tier > 0,
                    x => x.Tier,
                    true))
                .Where(x => RiasBot.Members.ContainsKey(x.UserId))
                .Select(x =>
                {
                    var user = RiasBot.Members[x.UserId];
                    return new PatreonDiscordUser
                    {
                        PatreonId = x.PatreonUserId,
                        DiscordId = x.UserId,
                        PatreonUsername = x.PatreonUserName,
                        DiscordUsername = user.Username,
                        DiscordDiscriminator = user.Discriminator,
                        DiscordAvatar = user.GetAvatarUrl(ImageFormat.Auto),
                        Tier = x.Tier
                    };
                });

            try
            {
                var data = JsonConvert.SerializeObject(patrons, Formatting.Indented);
#if RIAS_GLOBAL
                await _httpClient!.PostAsync("https://riasbot.me/api/patreon", new StringContent(data));
#elif DEBUG
                await _httpClient!.PostAsync("https://localhost/api/patreon", new StringContent(data));
#endif
            }
            catch
            {
            }
        }
    }
}