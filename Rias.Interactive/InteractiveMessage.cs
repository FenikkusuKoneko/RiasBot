using Discord;

namespace Rias.Interactive
{
    public class InteractiveMessage
    {
        public string? Content { get; internal set; }
        public EmbedBuilder? EmbedBuilder { get; internal set; }
        internal bool EmbedFooterSet { get; set; }

        public InteractiveMessage(string content)
        {
            Content = content;
        }

        public InteractiveMessage(EmbedBuilder embed)
        {
            EmbedBuilder = embed;
        }

        public InteractiveMessage(string content, EmbedBuilder embed)
        {
            Content = content;
            EmbedBuilder = embed;
        }
    }
}