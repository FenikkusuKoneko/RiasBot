using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Rias.Interactive.Criteria;
using Rias.Interactive.Paginator;

namespace Rias.Interactive
{
    public class InteractiveService
    {
        public string Version = "1.0.0";

        private readonly BaseSocketClient _client;
        private readonly PaginatorService _paginatorService;

        private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(1);

        public InteractiveService(BaseSocketClient client)
        {
            _client = client;

            client.ReactionAdded += ReactionAddedAsync;
            client.MessageDeleted += MessageDeletedAsync;

            _paginatorService = new PaginatorService(this);
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == _client.CurrentUser.Id)
                return;

            await _paginatorService.HandlePaginatedMessageAsync(reaction);
        }

        private async Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            await _paginatorService.RemovePaginatedMessageAsync(message.Id, true);
        }

        public async Task<SocketMessage> NextMessageAsync(IUserMessage userMessage,
            TimeSpan? timeout = null,
            bool fromSourceUser = true,
            bool fromSourceChannel = true)
        {
            var criterion = new Criterion<SocketMessage>();

            if (fromSourceUser)
                criterion.AddCriterion(new FromUserCriterion(userMessage.Author.Id));
            if (fromSourceChannel)
                criterion.AddCriterion(new SourceChannelCriterion());

            return await NextMessageAsync(userMessage, criterion, timeout);
        }

        public async Task<SocketMessage> NextMessageAsync(IUserMessage userMessage,
            ICriterion<SocketMessage> criterion,
            TimeSpan? timeout = null)
        {
            timeout ??= _defaultTimeout;

            var mtcs = new TaskCompletionSource<SocketMessage>();
            var ctcs = new TaskCompletionSource<bool>();

            async Task MessageReceivedAsync(SocketMessage message)
            {
                var result = await criterion.CheckAsync(userMessage, message);
                if (result) mtcs.SetResult(message);
            }

            _client.MessageReceived += MessageReceivedAsync;

            var messageTask = mtcs.Task;
            var cancelTask = ctcs.Task;
            var delay = Task.Delay(timeout.Value);
            var task = await Task.WhenAny(messageTask, delay, cancelTask).ConfigureAwait(false);

            _client.MessageReceived -= MessageReceivedAsync;

            if (task == messageTask)
                return await messageTask.ConfigureAwait(false);

            return null;
        }

        public async Task SendPaginatedMessageAsync(IUserMessage userMessage, PaginatedMessage message, TimeSpan? timeout = null)
            => await _paginatorService.CreatePaginatedMessage(userMessage, message, timeout);
    }
}