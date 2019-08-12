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
            if (reaction.UserId != _client.CurrentUser.Id)
                _ = _paginatorService.HandlePaginatedMessageAsync(reaction);

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
            var task = await Task.WhenAny(messageTask, delay);

            _client.MessageReceived -= MessageReceivedAsync;

            if (task == messageTask)
                return messageTask.Result;

            return null;
        }
        
        // Why not just make this return void? Or return the task directly and let the customer decide to discard or not
        public Task SendPaginatedMessageAsync(IUserMessage userMessage, PaginatedMessage message, TimeSpan? timeout = null)
        {
            _ = _paginatorService.CreatePaginatedMessage(userMessage, message, timeout);
            return Task.CompletedTask;
        }
    }
}
