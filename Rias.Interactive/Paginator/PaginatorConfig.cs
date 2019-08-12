using Discord;

namespace Rias.Interactive.Paginator
{
    public class PaginatorConfig
    {
        public static readonly PaginatorConfig Default = new PaginatorConfig();

        public readonly IEmote First = new Emoji("⏮");
        public readonly IEmote Back = new Emoji("◀");
        public readonly IEmote Next = new Emoji("▶");
        public readonly IEmote Last = new Emoji("⏭");
        public readonly IEmote Stop = new Emoji("⏹");
        public readonly IEmote Jump = new Emoji("🔢");

        public bool UseStop { get; set; }
        public bool UseJump { get; set; }
        public StopOptions StopOptions { get; set; } = StopOptions.None;

        public string FooterFormat { get; set; } = "Page {0}/{1}";
    }

    public enum StopOptions
    {
        None,
        SourceUser
    }
}