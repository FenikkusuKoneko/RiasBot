using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Configurations;
using Rias.Database;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;
using Serilog;

namespace Rias.Modules
{
    public abstract class RiasModule : ModuleBase<RiasCommandContext>, IAsyncDisposable
    {
        public readonly RiasBot RiasBot;
        public readonly Configuration Configuration;
        public readonly Localization Localization;
        public readonly RiasDbContext DbContext;
        
        private readonly IServiceScope _scope;
        private readonly Lazy<HttpClient> _httpClient;
        
        private DiscordMessageBuilder? _messageBuilder;
        private DiscordMessageBuilder MessageBuilder => _messageBuilder ??= new DiscordMessageBuilder();

        private TimeSpan _interactivityTimeout = TimeSpan.FromMinutes(1);

        public RiasModule(IServiceProvider serviceProvider)
        {
            RiasBot = serviceProvider.GetRequiredService<RiasBot>();
            Configuration = serviceProvider.GetRequiredService<Configuration>();
            Localization = serviceProvider.GetRequiredService<Localization>();
            
            _httpClient = new Lazy<HttpClient>(() => serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient());

            _scope = serviceProvider.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        }

        public HttpClient HttpClient => _httpClient.Value;
        
        /// <summary>
        /// Send a confirmation message with or without arguments. The form is an embed with the confirm color.<br/>
        /// </summary>
        public Task<DiscordMessage> ReplyConfirmationAsync(string key, params object[] args)
            => Context.Channel.SendConfirmationMessageAsync(Localization.GetText(Context.Guild?.Id, key, args));

        /// <summary>
        /// Send an error message with or without arguments. The form is an embed with the error color.<br/>
        /// </summary>
        public Task<DiscordMessage> ReplyErrorAsync(string key, params object[] args)
            => Context.Channel.SendErrorMessageAsync(Localization.GetText(Context.Guild?.Id, key, args));

        public async Task<DiscordMessage> ReplyAsync(DiscordEmbedBuilder embed)
            => await Context.Channel.SendMessageAsync(embed);

        public Task SendPaginatedMessageAsync<T>(IList<T> items, int itemsPerPage, Func<IEnumerable<T>, int, DiscordEmbedBuilder> embedFunc)
            => SendPaginatedMessageAsync(items, itemsPerPage, null, embedFunc);

        public async Task SendPaginatedMessageAsync<T>(IList<T> items, int itemsPerPage, string? content, Func<IEnumerable<T>, int, DiscordEmbedBuilder> embedFunc)
        {
            var pageCount = (items.Count - 1) / itemsPerPage + 1;
            
            var pages = items.Split(itemsPerPage).Select((x, i) =>
            {
                var embed = embedFunc(x, itemsPerPage * i);
                var footerText = GetText(Localization.CommonMenuPage, i + 1, pageCount);
                if (embed.Footer != null)
                    footerText += $" | {embed.Footer.Text}";
                embed.WithFooter(footerText);
                return new Page(content, embed);
            });

            await Context.Interactivity.SendPaginatedMessageAsync(Context.Channel, Context.User, pages, null, timeoutoverride: _interactivityTimeout);
        }

        public Task<InteractivityResult<ComponentInteractionCreateEventArgs>?> SendConfirmationButtonsAsync(string key, params object[] args)
            => SendConfirmationButtonsAsync(MessageBuilder.WithEmbed(new DiscordEmbedBuilder().WithColor(RiasUtilities.Yellow).WithDescription(GetText(key, args))));

        public async Task<InteractivityResult<ComponentInteractionCreateEventArgs>?> SendConfirmationButtonsAsync(DiscordMessageBuilder messageBuilder)
        {
            _messageBuilder = messageBuilder;
            messageBuilder.AddComponents(new DiscordButtonComponent(ButtonStyle.Success, "yes", GetText(Localization.CommonYes)),
                new DiscordButtonComponent(ButtonStyle.Danger, "no", GetText(Localization.CommonNo)));
            
            var message = await Context.Channel.SendMessageAsync(messageBuilder);
            var componentInteractionArgs = await message.WaitForButtonAsync(Context.User, timeoutOverride: _interactivityTimeout);
            if (componentInteractionArgs.TimedOut || string.Equals(componentInteractionArgs.Result.Id, "no"))
            {
                if (messageBuilder.Files.Count > 0)
                    ((List<DiscordMessageFile>) messageBuilder.Files).Clear();
                
                ((List<DiscordActionRowComponent>) messageBuilder.Components).Clear();
                await messageBuilder.WithEmbed(new DiscordEmbedBuilder(messageBuilder.Embed)
                        .WithColor(RiasUtilities.Red)
                        .WithFooter(GetText(Localization.CommonActionCanceled)))
                    .ModifyAsync(message);
                
                return null;
            }

            return componentInteractionArgs;
        }
        
        public Task ConfirmButtonsActionAsync(DiscordMessage message)
        {
            if (MessageBuilder.Files.Count > 0)
                ((List<DiscordMessageFile>) MessageBuilder.Files).Clear();
            
            ((List<DiscordActionRowComponent>) MessageBuilder.Components).Clear();
            return MessageBuilder.WithEmbed(new DiscordEmbedBuilder(MessageBuilder.Embed)
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithFooter(GetText(Localization.CommonActionCompleted)))
                .ModifyAsync(message);
        }
        
        public Task ButtonsActionModifyDescriptionAsync(DiscordMessage message, string key, params object[] args)
        {
            if (MessageBuilder.Files.Count > 0)
                ((List<DiscordMessageFile>) MessageBuilder.Files).Clear();
            
            ((List<DiscordActionRowComponent>) MessageBuilder.Components).Clear();
            return MessageBuilder.WithEmbed(new DiscordEmbedBuilder(MessageBuilder.Embed)
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithDescription(GetText(key, args)))
                .ModifyAsync(message);
        }

        public Task ButtonsActionModifyEmbedAsync(DiscordMessage message, DiscordEmbed embed)
        {
            if (MessageBuilder.Files.Count > 0)
                
                ((List<DiscordMessageFile>) MessageBuilder.Files).Clear();
            ((List<DiscordActionRowComponent>) MessageBuilder.Components).Clear();
            return MessageBuilder.WithEmbed(embed).ModifyAsync(message);
        }

        /// <summary>
        /// Get a translation text with or without arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public string GetText(string key, params object[] args)
            => Localization.GetText(Context.Guild?.Id, key, args);

        public async ValueTask DisposeAsync()
        {
            if (_scope is IAsyncDisposable asyncDisposableScope)
                await asyncDisposableScope.DisposeAsync();
            else
                _scope.Dispose();
            
            Log.Debug("Module: {Module}, Command: {Command}, scope disposed", Context.Command.Module.Name, Context.Command.Name);
        }
    }

    public abstract class RiasModule<TService> : RiasModule
        where TService : RiasService
    {
        public readonly TService Service;

        public RiasModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Service = serviceProvider.GetRequiredService<TService>();
        }
    }
}