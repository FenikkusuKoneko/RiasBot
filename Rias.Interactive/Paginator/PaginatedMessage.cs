using System;
using System.Collections.Generic;
using Discord;

namespace Rias.Interactive.Paginator
{
    public class PaginatedMessage
    {
        internal IUser SourceUser { get; set; }
        internal IUserMessage Message { get; set; }
        internal int CurrentPage { get; set; }
        internal bool JumpActivated { get; set; }

        public IEnumerable<EmbedBuilder> Pages { get; set; }
        public PaginatorConfig Config { get; set; } = PaginatorConfig.Default;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }
}