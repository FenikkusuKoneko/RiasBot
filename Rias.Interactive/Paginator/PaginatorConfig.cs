using Discord;

namespace Rias.Interactive.Paginator
{
    public class PaginatorConfig
    {
        public static readonly PaginatorConfig Default = new PaginatorConfig();

        public IEmote First { get; set; } = new Emoji("⏮");
        public IEmote Back { get; set; } = new Emoji("◀");
        public IEmote Next { get; set; } = new Emoji("▶");
        public IEmote Last { get; set; } = new Emoji("⏭");
        public IEmote Stop { get; set; } = new Emoji("⏹");
        public IEmote Jump { get; set; } = new Emoji("🔢");

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