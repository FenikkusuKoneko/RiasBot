using System;

namespace Rias.Core.Services.Commons
{
    public enum OutputChannelState
    {
        Null,
        Available,
        NoViewPermission,
        NoSendPermission
    }

    public class YoutubeUrl
    {
        public string? VideoId { get; set; }
        public string? ListId { get; set; }
    }

    [Flags]
    public enum PlayerPatreonFeatures
    {
        None = 0,
        Volume = 1,
        LongTracks = 2,
        Livestream = 4
    }
}