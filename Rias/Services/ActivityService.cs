using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Rias.Services
{
    public class ActivityService : RiasService
    {
        public ActivityService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        private Timer? _activityTimer;
        private DiscordActivity[]? _activities;
        private int _activityIndex;

        public DiscordActivity GetActivity(string name, string type)
        {
            if (!Enum.TryParse<ActivityType>(type, true, out var activityType))
                return new DiscordActivity(name, ActivityType.Playing);

            if (activityType != ActivityType.Streaming)
                return new DiscordActivity(name, activityType);

            var streamUrlIndex = name.IndexOf(" ", StringComparison.Ordinal);
            if (streamUrlIndex <= 0)
                return new DiscordActivity(name, ActivityType.Playing);
            
            var streamUrl = name[..streamUrlIndex];
            if (Uri.IsWellFormedUriString(streamUrl, UriKind.Absolute))
                return new DiscordActivity(name[streamUrlIndex..].TrimStart(), ActivityType.Streaming) {StreamUrl = streamUrl};
            
            return new DiscordActivity(name, ActivityType.Playing);
        }

        public Task StartActivityRotationAsync(TimeSpan period, IEnumerable<DiscordActivity> activities)
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
            
            return RiasBot.Client.UpdateStatusAsync(_activities[_activityIndex++], RiasBot.Client.CurrentUser.Presence.Status);
        }
    }
}