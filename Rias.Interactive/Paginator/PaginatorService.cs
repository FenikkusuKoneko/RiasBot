using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Rias.Interactive.Criteria;

namespace Rias.Interactive.Paginator
{
    internal class PaginatorService
    {
        private readonly InteractiveService _interactive;

        private readonly ConcurrentDictionary<ulong, PaginatedMessage> _messages = new ConcurrentDictionary<ulong, PaginatedMessage>();
        private readonly ConcurrentDictionary<ulong, Timer> _timeouts = new ConcurrentDictionary<ulong, Timer>();

        public PaginatorService(InteractiveService interactive)
        {
            _interactive = interactive;
        }

        public async Task CreatePaginatedMessage(IUserMessage userMessage, PaginatedMessage paginatedMessage, TimeSpan? timeout)
        {
            if (paginatedMessage.Pages is null)
                throw new NullReferenceException("The pages collection cannot be null");

            if (!paginatedMessage.Pages.Any())
                throw new ArgumentException("The pages collection cannot be empty");

            paginatedMessage.SourceUser = userMessage.Author;

            var message = await RenderPageAsync(paginatedMessage, 0, userMessage.Channel);
            paginatedMessage.Message = message;

            if (timeout.HasValue && timeout.Value > TimeSpan.Zero)
                paginatedMessage.Timeout = timeout.Value;

            var paginatedConfig = paginatedMessage.Config;

            _ = Task.Run(async () =>
            {
                var emotes = new List<IEmote>
                {
                    paginatedConfig.First,
                    paginatedConfig.Back,
                    paginatedConfig.Next,
                    paginatedConfig.Last
                };

                if (paginatedConfig.UseStop)
                    emotes.Add(paginatedConfig.Stop);

                if (paginatedConfig.UseJump)
                    emotes.Add(paginatedConfig.Jump);

                await message.AddReactionsAsync(emotes.ToArray());
            });

            AddPaginatedMessage(message.Id, paginatedMessage);
        }

        public void AddPaginatedMessage(ulong messageId, PaginatedMessage message)
        {
            _messages[messageId] = message;
            _timeouts[messageId] = new Timer(async _ => await RemovePaginatedMessageAsync(messageId),
                null, message.Timeout, TimeSpan.Zero);
        }

        public async Task RemovePaginatedMessageAsync(ulong messageId, bool messageDeleted = false)
        {
            _messages.TryRemove(messageId, out var paginatedMessage);
            _timeouts.TryRemove(messageId, out var timer);

            if (timer != null)
                await timer.DisposeAsync();

            if (!messageDeleted)
                await paginatedMessage.Message.RemoveAllReactionsAsync();
        }

        public async Task HandlePaginatedMessageAsync(SocketReaction reaction)
        {
            if (!_messages.TryGetValue(reaction.MessageId, out var paginatedMessage))
                return;

            var emote = reaction.Emote;
            var message = paginatedMessage.Message;
            var paginatedConfig = paginatedMessage.Config;
            var page = paginatedMessage.CurrentPage;
            var pagesCount = paginatedMessage.Pages.Count();

            if (emote.Equals(paginatedConfig.First))
            {
                if (page > 0)
                {
                    page = 0;
                    await RenderPageAsync(paginatedMessage, page);
                    await RemoveReactionAsync(message, reaction);
                    return;
                }
            }

            if (emote.Equals(paginatedConfig.Back))
            {
                if (page > 0)
                {
                    page--;
                    await RenderPageAsync(paginatedMessage, page);
                    await RemoveReactionAsync(message, reaction);
                    return;
                }
            }

            if (emote.Equals(paginatedConfig.Next))
            {
                if (page < pagesCount - 1)
                {
                    page++;
                    await RenderPageAsync(paginatedMessage, page);
                    await RemoveReactionAsync(message, reaction);
                    return;
                }
            }

            if (emote.Equals(paginatedConfig.Last))
            {
                if (page < pagesCount - 1)
                {
                    page = pagesCount - 1;
                    await RenderPageAsync(paginatedMessage, page);
                    await RemoveReactionAsync(message, reaction);
                    return;
                }
            }

            if (emote.Equals(paginatedConfig.Stop))
            {
                if (paginatedConfig.StopOptions == StopOptions.None
                    || paginatedConfig.StopOptions == StopOptions.SourceUser
                    && reaction.UserId == paginatedMessage.SourceUser.Id)
                {
                    await message.DeleteAsync();
                    await RemovePaginatedMessageAsync(message.Id, true);
                    return;
                }
            }

            if (emote.Equals(paginatedConfig.Jump) && !paginatedMessage.JumpActivated)
            {
                _ = Task.Run(async () =>
                {
                    var criterion = new Criterion<SocketMessage>()
                        .AddCriterion(new FromUserCriterion(reaction.UserId))
                        .AddCriterion(new SourceChannelCriterion())
                        .AddCriterion(new IsIntegerCriterion());

                    var input = await _interactive.NextMessageAsync(paginatedMessage.Message, criterion, TimeSpan.FromSeconds(15));
                    paginatedMessage.JumpActivated = true;
                    if (!(input is null))
                    {
                        page = int.Parse(input.Content) - 1;
                        await input.DeleteAsync();

                        if (page < 0)
                            page = 0;

                        if (page > pagesCount)
                            page = pagesCount - 1;

                        await RenderPageAsync(paginatedMessage, page);
                        await RemoveReactionAsync(message, reaction);

                        paginatedMessage.JumpActivated = false;
                    }

                    if (input is null)
                    {
                        paginatedMessage.JumpActivated = false;
                    }
                });

                return;
            }

            await RemoveReactionAsync(message, reaction);
        }

        private async Task RemoveReactionAsync(IUserMessage message, SocketReaction reaction)
        => await message.RemoveReactionAsync(reaction.Emote, reaction.UserId);

        private async Task<IUserMessage> RenderPageAsync(PaginatedMessage message, int pagePosition, IMessageChannel channel = null)
        {
            message.CurrentPage = pagePosition;

            var page = message.Pages.ElementAt(pagePosition);

            var footer = string.Format(message.Config.FooterFormat, pagePosition + 1, message.Pages.Count());
            if (!string.IsNullOrWhiteSpace(page.Footer?.Text))
                footer = $"{footer} | {page.Footer.Text}";

            page.WithFooter(new EmbedFooterBuilder
            {
                IconUrl = page.Footer?.IconUrl,
                Text = footer
            });

            if (channel != null)
            {
                return await channel.SendMessageAsync(embed: page.Build());
            }

            await message.Message.ModifyAsync(x => x.Embed = page.Build());
            return message.Message;
        }
    }
}