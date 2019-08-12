using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Rias.Interactive.Paginator;

namespace Rias.Interactive
{
    public class InteractiveService
    {
        private readonly BaseSocketClient _client;
        private readonly PaginatorService _paginatorService;

        public readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

        public InteractiveService(BaseSocketClient client)
        {
            _client = client;

            client.ReactionAdded += ReactionAddedAsync;
            client.MessageDeleted += MessageDeletedAsync;

            _paginatorService = new PaginatorService(this);
        }

        private Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == _client.CurrentUser.Id)
                return Task.CompletedTask;

            _ = Task.Run(() => _paginatorService.HandlePaginatedMessageAsync(reaction));

            return Task.CompletedTask;
        }

        private Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
            => _paginatorService.RemovePaginatedMessageAsync(message.Id, true);

        public Task<SocketMessage> NextMessageAsync(IUserMessage userMessage,
            bool fromSourceUser = true,
            bool fromSourceChannel = true,
            TimeSpan? timeout = null)
            => NextMessageAsync(userMessage, (x, y) =>
            {
                var ret = true;
                if (fromSourceUser)
                    ret = x.Author.Id == y.Author.Id;
                if (fromSourceChannel)
                    ret = ret && x.Channel.Id == y.Channel.Id;

                return ret;
            }, timeout);

        public async Task<SocketMessage> NextMessageAsync(IUserMessage userMessage,
            Func<IUserMessage, SocketMessage, bool> predicate,
            TimeSpan? timeout = null)
        {
            timeout ??= DefaultTimeout;

            var mtcs = new TaskCompletionSource<SocketMessage>();
            Task MessageReceivedAsync(SocketMessage message)
            {
                if (predicate.Invoke(userMessage, message))
                    mtcs.SetResult(message);

                return Task.CompletedTask;
            }

            _client.MessageReceived += MessageReceivedAsync;

            var messageTask = mtcs.Task;
            var delay = Task.Delay(timeout.Value);
            var task = await Task.WhenAny(messageTask, delay).ConfigureAwait(false);

            _client.MessageReceived -= MessageReceivedAsync;

            if (task == messageTask)
                return await messageTask.ConfigureAwait(false);

            return null;
        }

        public Task SendPaginatedMessageAsync(IUserMessage userMessage, PaginatedMessage message, TimeSpan? timeout = null)
        {
            _ = Task.Run(() => _paginatorService.CreatePaginatedMessage(userMessage, message, timeout));
            return Task.CompletedTask;
        }
    }
}