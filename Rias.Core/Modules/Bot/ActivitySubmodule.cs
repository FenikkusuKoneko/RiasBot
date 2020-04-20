using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Bot
{
    public partial class BotModule
    {
        [Name("Activity")]
        public class ActivitySubmodule : RiasModule<ActivityService>
        {
            public ActivitySubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("activity"), OwnerOnly,
             Priority(0)]
            public async Task ActivityAsync(string? type = null, [Remainder] string? name = null)
            {
                await Service.StopActivityRotationAsync();
                
                if (type is null || name is null)
                {
                    await RiasBot.SetPresenceAsync(null);
                    await ReplyConfirmationAsync(Localization.BotActivityRemoved);
                    return;
                }

                var activity = Service.GetActivity(name, type);
                if (activity.Type == ActivityType.Streaming)
                    name = $"[{name}]({activity.Url})";

                await RiasBot.SetPresenceAsync(activity);
                await ReplyConfirmationAsync(Localization.BotActivitySet, GetText(Localization.BotActivity(type.ToLower()), name.ToLowerInvariant()).ToLowerInvariant());
            }
            
            [Command("activity"), OwnerOnly, Priority(1)]
            public async Task ActivityAsync(int period, [Remainder] string activities)
            {
                if (period < 12)
                {
                    await ReplyErrorAsync(Localization.BotActivityRotationLimit, 12);
                    return;
                }
                
                var activitiesEnumerable = activities.Split("\n").Select(x =>
                {
                    var typeIndex = x.IndexOf(" ", StringComparison.Ordinal);
                    return typeIndex <= 0 ? new LocalActivity(x, ActivityType.Playing) : Service.GetActivity(x[typeIndex..].TrimStart(), x[..typeIndex]);
                }).ToArray();
                await Service.StartActivityRotationAsync(TimeSpan.FromSeconds(period), activitiesEnumerable);
                await ReplyConfirmationAsync(Localization.BotActivityRotationSet, period, string.Join("\n", activitiesEnumerable.Select(x => $"{x.Type} {x.Name}")));
            }
            
            [Command("status"), OwnerOnly]
            public async Task SetStatusAsync(string status)
            {
                if (string.Equals(status, "dnd", StringComparison.InvariantCultureIgnoreCase))
                    status = "DoNotDisturb";

                if (!Enum.TryParse<UserStatus>(status, true, out var userStatus))
                    userStatus = UserStatus.Online;

                if (userStatus == UserStatus.Offline)
                    userStatus = UserStatus.Invisible;
                
                await RiasBot.SetPresenceAsync(userStatus);
                await ReplyConfirmationAsync(Localization.BotStatusSet, GetText(Localization.BotStatus(userStatus.Humanize(LetterCasing.LowerCase).Underscore())).ToLowerInvariant());
            }
        }
    }
}