using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Discord;

namespace Rias.Interactive.Paginator
{
    public class PaginatedMessage
    {
        internal IUserMessage? SourceUserMessage { get; set; }
        internal IUserMessage? Message { get; set; }
        internal int CurrentPage { get; set; }
        internal bool JumpActivated { get; set; }
        internal CancellationTokenSource Cts { get; }
        internal readonly PaginatorConfig Config;

        public readonly IList<InteractiveMessage> Pages;

        public PaginatedMessage(IEnumerable<InteractiveMessage> pages, PaginatorConfig? config = null)
        {
            Pages = pages.ToList();
            Config = config ?? PaginatorConfig.Default;
            Cts = new CancellationTokenSource();
        }
    }
}