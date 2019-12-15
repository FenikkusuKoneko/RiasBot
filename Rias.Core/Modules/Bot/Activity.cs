using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Services;

namespace Rias.Core.Modules.Bot
{
    public partial class Bot
    {
        [Name("Activity")]
        public class Activity : RiasModule<ActivityService>
        {
            private readonly DiscordShardedClient _client;

            public Activity(IServiceProvider services) : base(services)
            {
                _client = services.GetRequiredService<DiscordShardedClient>();
            }

            [Command("activity"),
             OwnerOnly, Priority(0)]
            public async Task ActivityAsync(string? type = null, [Remainder] string? name = null)
            {
                await Service.StopActivityRotationAsync();
                
                if (type is null || name is null)
                {
                    await _client.SetActivityAsync(null);
                    await ReplyConfirmationAsync("ActivityRemoved");
                    return;
                }

                var activity = Service.GetActivity(name, type);
                if (activity is StreamingGame streamingGame)
                    name = $"[{name}]({streamingGame.Url})";

                await _client.SetActivityAsync(activity);
                await ReplyConfirmationAsync("ActivitySet", GetText($"Activity{type.Titleize()}", name.ToLowerInvariant()).ToLowerInvariant());
            }

            [Command("activity"),
             OwnerOnly, Priority(1)]
            public async Task ActivityAsync(int period, [Remainder] string activities)
            {
                if (period < 12)
                {
                    await ReplyErrorAsync("ActivityRotationLimit", 12);
                    return;
                }
                
                var activitiesEnumerable = activities.Split("\n").Select(x =>
                {
                    var typeIndex = x.IndexOf(" ", StringComparison.Ordinal);
                    return typeIndex <= 0 ? new Game(x) : Service.GetActivity(x[typeIndex..].TrimStart(), x[..typeIndex]);
                }).ToArray();
                await Service.StartActivityRotationAsync(TimeSpan.FromSeconds(period), activitiesEnumerable);
                await ReplyConfirmationAsync("ActivityRotationSet", period, string.Join("\n", activitiesEnumerable.Select(x => $"{x.Type} {x.Name}")));
            }

            [Command("status"),
             OwnerOnly]
            public async Task SetStatusAsync(string status)
            {
                if (string.Equals(status, "dnd", StringComparison.InvariantCultureIgnoreCase))
                    status = "DoNotDisturb";

                if (!Enum.TryParse<UserStatus>(status, true, out var userStatus))
                    userStatus = UserStatus.Online;

                if (userStatus == UserStatus.Offline)
                    userStatus = UserStatus.Invisible;
                
                await _client.SetStatusAsync(userStatus);
                await ReplyConfirmationAsync("StatusSet", GetText($"Status{userStatus.ToString()}").ToLowerInvariant());
            }
        }
    }
}