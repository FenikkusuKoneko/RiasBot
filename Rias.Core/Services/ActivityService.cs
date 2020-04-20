using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;

namespace Rias.Core.Services
{
    public class ActivityService : RiasService
    {
        public ActivityService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        private Timer? _activityTimer;
        private LocalActivity[]? _activities;
        private int _activityIndex;

        public LocalActivity GetActivity(string name, string type)
        {
            if (!Enum.TryParse<ActivityType>(type, true, out var activityType))
                return new LocalActivity(name, ActivityType.Playing);

            if (activityType != ActivityType.Streaming)
                return new LocalActivity(name, activityType);

            var streamUrlIndex = name.IndexOf(" ", StringComparison.Ordinal);
            if (streamUrlIndex <= 0)
                return new LocalActivity(name, ActivityType.Playing);
            
            var streamUrl = name[..streamUrlIndex];
            if (Uri.IsWellFormedUriString(streamUrl, UriKind.Absolute))
                return new LocalActivity(name[streamUrlIndex..].TrimStart(), streamUrl);

            return new LocalActivity(name, ActivityType.Playing);
        }

        public Task StartActivityRotationAsync(TimeSpan period, IEnumerable<LocalActivity> activities)
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
            
            return RiasBot.SetPresenceAsync(_activities[_activityIndex++]);
        }
    }
}