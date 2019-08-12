using System;
using System.Collections.Generic;
using System.Threading;
using Discord;

namespace Rias.Interactive.Paginator
{
    public class PaginatedMessage
    {
        internal IUserMessage SourceUserMessage { get; set; }
        internal IUserMessage Message { get; set; }
        internal int CurrentPage { get; set; }
        internal bool JumpActivated { get; set; }
        internal CancellationTokenSource Cts;

        public readonly IEnumerable<InteractiveMessage> Pages;
        public readonly PaginatorConfig Config;

        public PaginatedMessage(IEnumerable<InteractiveMessage> pages, PaginatorConfig config = null)
        {
            Pages = pages;
            Config = config ?? PaginatorConfig.Default;
            Cts = new CancellationTokenSource();
        }
    }
}