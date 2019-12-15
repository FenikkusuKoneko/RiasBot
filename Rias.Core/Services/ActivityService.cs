using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Rias.Core.Services
{
    public class ActivityService : RiasService
    {
        private readonly DiscordShardedClient _client;

        public ActivityService(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
        }
        
        private Timer? _activityTimer;
        private IActivity[]? _activities;
        private int _activityIndex;

        public IActivity GetActivity(string name, string type)
        {
            if (!Enum.TryParse<ActivityType>(type, true, out var activityType))
                return new Game(name);

            if (activityType != ActivityType.Streaming)
                return new Game(name, activityType);

            var streamUrlIndex = name.IndexOf(" ", StringComparison.Ordinal);
            if (streamUrlIndex <= 0)
                return new Game(name);
            
            var streamUrl = name[..streamUrlIndex];
            if (Uri.IsWellFormedUriString(streamUrl, UriKind.Absolute))
                return new StreamingGame(name[streamUrlIndex..].TrimStart(), streamUrl);

            return new Game(name);
        }

        public Task StartActivityRotationAsync(TimeSpan period, IEnumerable<IActivity> activities)
        {
            _activities = activities.ToArray();
            _activityTimer = new Timer(async _ => await SetNextActivityAsync(), null, TimeSpan.Zero, period);
            return Task.CompletedTask;
        }
        
        public Task StopActivityRotationAsync()
        {
            _activityTimer?.Dispose();
            return Task.CompletedTask;
        }

        private Task SetNextActivityAsync()
        {
            if (_activityIndex == _activities!.Length)
                _activityIndex = 0;
            
            return _client.SetActivityAsync(_activities[_activityIndex++]);
        }
    }
}