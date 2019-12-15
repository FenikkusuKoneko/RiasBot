using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Rias.Interactive.Paginator
{
    internal class PaginatorService
    {
        private readonly InteractiveService _interactive;

        private readonly ConcurrentDictionary<ulong, PaginatedMessage> _messages = new ConcurrentDictionary<ulong, PaginatedMessage>();

        public PaginatorService(InteractiveService interactive)
        {
            _interactive = interactive;
        }

        public async Task CreatePaginatedMessage(IUserMessage userMessage, PaginatedMessage paginatedMessage, TimeSpan? timeout)
        {
            if (paginatedMessage.Pages is null)
                throw new NullReferenceException("The pages collection cannot be null");

            if (paginatedMessage.Pages.Count == 0)
                throw new ArgumentException("The pages collection cannot be empty");

            paginatedMessage.SourceUserMessage = userMessage;

            var message = await RenderPageAsync(paginatedMessage, 0, userMessage.Channel);
            paginatedMessage.Message = message;

            var paginatedConfig = paginatedMessage.Config;

            _ = Task.Run(async () =>
            {
                if (paginatedMessage.Pages.Count > 1)
                {
                    await message.AddReactionAsync(paginatedConfig.First);
                    await message.AddReactionAsync(paginatedConfig.Back);
                    await message.AddReactionAsync(paginatedConfig.Next);
                    await message.AddReactionAsync(paginatedConfig.Last);
                }

                if (paginatedConfig.UseStop)
                    await message.AddReactionAsync(paginatedConfig.Stop);

                if (paginatedMessage.Pages.Count > 1 && paginatedConfig.UseJump)
                    await message.AddReactionAsync(paginatedConfig.Jump);
            });

            var cancelMessage = new TaskCompletionSource<bool>();
            var token = paginatedMessage.Cts.Token;
            token.Register(() => cancelMessage.SetResult(true));

            AddPaginatedMessage(message.Id, paginatedMessage);

            var delay = Task.Delay(timeout ?? _interactive.DefaultTimeout);
            var task = await Task.WhenAny(delay, cancelMessage.Task);

            if (task == delay)
                await RemovePaginatedMessageAsync(message.Id);
        }

        public void AddPaginatedMessage(ulong messageId, PaginatedMessage message)
        {
            _messages[messageId] = message;
        }

        public Task RemovePaginatedMessageAsync(ulong messageId, bool deleted = false)
        {
            if (!_messages.TryRemove(messageId, out var paginatedMessage))
                return Task.CompletedTask;

            if (!deleted) return paginatedMessage.Message!.RemoveAllReactionsAsync();

            paginatedMessage.Cts.Cancel();
            paginatedMessage.Cts.Dispose();
            return Task.CompletedTask;
        }

        public async Task HandlePaginatedMessageAsync(SocketReaction reaction)
        {
            if (!_messages.TryGetValue(reaction.MessageId, out var paginatedMessage))
                return;

            var emote = reaction.Emote;
            var message = paginatedMessage.Message!;
            var paginatedConfig = paginatedMessage.Config;
            var page = paginatedMessage.CurrentPage;
            var pagesCount = paginatedMessage.Pages.Count;

            if (emote.Equals(paginatedConfig.First) && page > 0)
                page = 0;

            if (emote.Equals(paginatedConfig.Back) && page > 0)
                page--;

            if (emote.Equals(paginatedConfig.Next) && page < pagesCount - 1)
                page++;

            if (emote.Equals(paginatedConfig.Last) && page < pagesCount - 1)
                page = pagesCount - 1;

            if (emote.Equals(paginatedConfig.Stop))
            {
                if (paginatedConfig.StopOptions == StopOptions.None
                    || paginatedConfig.StopOptions == StopOptions.SourceUser
                    && reaction.UserId == paginatedMessage.SourceUserMessage!.Author.Id)
                {
                    await paginatedMessage.Message!.DeleteAsync();
                    return;
                }
            }

            if (emote.Equals(paginatedConfig.Jump) && !paginatedMessage.JumpActivated)
            {
                var input = await _interactive.NextMessageAsync(paginatedMessage.SourceUserMessage!, timeout: TimeSpan.FromSeconds(15));
                paginatedMessage.JumpActivated = true;
                if (input != null)
                {
                    if (int.TryParse(input.Content, out var number))
                    {
                        page = number - 1;
                        await input.DeleteAsync();
                    }

                    if (page < 0)
                        page = 0;

                    if (page > pagesCount)
                        page = pagesCount - 1;
                }

                paginatedMessage.JumpActivated = false;
            }

            if (paginatedMessage.CurrentPage != page)
                await RenderPageAsync(paginatedMessage, page);
            await RemoveReactionAsync(message, reaction);
        }

        private Task RemoveReactionAsync(IUserMessage message, SocketReaction reaction)
        => message.RemoveReactionAsync(reaction.Emote, reaction.UserId);

        private async Task<IUserMessage> RenderPageAsync(PaginatedMessage message, int pagePosition, IMessageChannel? channel = null)
        {
            message.CurrentPage = pagePosition;

            var page = message.Pages[pagePosition];
            var embed = page.EmbedBuilder;

            var footer = string.Format(message.Config.FooterFormat, pagePosition + 1, message.Pages.Count);
            if (embed != null && !string.IsNullOrEmpty(embed.Footer?.Text))
                footer = $"{footer} | {embed.Footer.Text}";

            if (embed is null)
            {
                page.EmbedBuilder = embed = new EmbedBuilder();
            }

            if (!page.EmbedFooterSet)
            {
                embed.WithFooter(footer);
                page.EmbedFooterSet = true;
            }

            if (channel != null)
            {
                return await channel.SendMessageAsync(page.Content, embed: embed.Build());
            }

            await message.Message!.ModifyAsync(x =>
            {
                x.Content = page.Content;
                x.Embed = embed.Build();
            });

            return message.Message!;
        }
    }
}