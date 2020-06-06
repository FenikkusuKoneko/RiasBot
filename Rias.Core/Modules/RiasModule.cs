using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Events;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Database;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Serilog;

namespace Rias.Core.Modules
{
    public abstract class RiasModule : ModuleBase<RiasCommandContext>, IAsyncDisposable
    {
        public readonly Rias RiasBot;
        public readonly Credentials Credentials;
        public readonly Localization Localization;
        public readonly RiasDbContext DbContext;
        
        private readonly InteractivityExtension _interactivity;
        private readonly IServiceScope _scope;
        
        private readonly TimeSpan _defaultInteractivityTimeout = TimeSpan.FromMinutes(1);

        public RiasModule(IServiceProvider serviceProvider)
        {
            RiasBot = serviceProvider.GetRequiredService<Rias>();
            Credentials = serviceProvider.GetRequiredService<Credentials>();
            Localization = serviceProvider.GetRequiredService<Localization>();

            _interactivity = serviceProvider.GetRequiredService<InteractivityExtension>();
            _scope = serviceProvider.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        }

        /// <summary>
        /// Send a confirmation message with or without arguments. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        public Task<IUserMessage> ReplyConfirmationAsync(string key, params object[] args)
        {
            return Context.Channel.SendConfirmationMessageAsync(Localization.GetText(Context.Guild?.Id, key, args));
        }

        /// <summary>
        /// Send an error message with or without arguments. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public Task<IUserMessage> ReplyErrorAsync(string key, params object[] args)
        {
            return Context.Channel.SendErrorMessageAsync(Localization.GetText(Context.Guild?.Id, key, args));
        }

        public async Task<IUserMessage> ReplyAsync(LocalEmbedBuilder embed)
            => await Context.Channel.SendMessageAsync(embed);

        public async Task SendPaginatedMessageAsync<T>(List<T> items, int itemsPerPage, Func<IEnumerable<T>, int, LocalEmbedBuilder> embedFunc)
        {
            var pageCount = (items.Count - 1) / itemsPerPage + 1;
            
            var pages = items.Split(itemsPerPage).Select((x, i) =>
            {
                var embed = embedFunc(x, itemsPerPage * i);
                var footerText = GetText(Localization.CommonMenuPage, i + 1, pageCount);
                if (embed.Footer != null)
                    footerText += $" | {embed.Footer.Text}";
                embed.WithFooter(footerText);
                return new Page(embed.Build());
            });
            
            var menu = new RiasPagedMenu(Context.User.Id, new DefaultPageProvider(pages));
            if (pageCount > 1)
            {
                await menu.AddButtonAsync(new Button(new LocalEmoji("⏮️"), async _ => await menu.ChangePageAsync(0), 0));
                await menu.AddButtonAsync(new Button(new LocalEmoji("◀️"), async _ => await menu.ChangePageAsync(menu.CurrentPageIndex - 1), 1));
                await menu.AddButtonAsync(new Button(new LocalEmoji("▶️"), async _ => await menu.ChangePageAsync(menu.CurrentPageIndex + 1), 2));
                await menu.AddButtonAsync(new Button(new LocalEmoji("⏭️"), async _ => await menu.ChangePageAsync(menu.PageProvider.PageCount - 1), 3));
            }

            await _interactivity.StartMenuAsync(Context.Channel, menu, _defaultInteractivityTimeout);
        }

        public Task<MessageReceivedEventArgs> NextMessageAsync()
        => _interactivity.WaitForMessageAsync(x => x.Message.Author.Id == Context.User.Id,
            _defaultInteractivityTimeout);

        /// <summary>
        /// Get a translation text with or without arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public string GetText(string key, params object[] args)
        {
            return Localization.GetText(Context.Guild?.Id, key, args);
        }
        
        /// <summary>
        /// Run a task in an async way.
        /// </summary>
        /// <param name="func"></param>
        public Task RunTaskAsync(Task func)
        {
            Task.Run(async () =>
            {
                try
                {
                    await func;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_scope is IAsyncDisposable asyncDisposableScope)
                await asyncDisposableScope.DisposeAsync();
            else
                _scope.Dispose();
            
            Log.Debug($"Module: {Context.Command.Module.Name}, Command: {Context.Command.Name}, scope disposed");
        }
    }

    public abstract class RiasModule<TService> : RiasModule where TService : RiasService
    {
        public readonly TService Service;

        public RiasModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Service = serviceProvider.GetRequiredService<TService>();
        }
    }
}