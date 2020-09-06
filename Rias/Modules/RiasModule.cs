using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Configuration;
using Rias.Database;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;
using Serilog;

namespace Rias.Modules
{
    public abstract class RiasModule : ModuleBase<RiasCommandContext>, IAsyncDisposable
    {
        private readonly RiasBot _riasBot;
        private readonly Credentials _credentials;
        private readonly Localization _localization;
        private readonly RiasDbContext _dbContext;
        
        private readonly IServiceScope _scope;
        
        public RiasModule(IServiceProvider serviceProvider)
        {
            _riasBot = serviceProvider.GetRequiredService<RiasBot>();
            _credentials = serviceProvider.GetRequiredService<Credentials>();
            _localization = serviceProvider.GetRequiredService<Localization>();

            _scope = serviceProvider.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        }

        public RiasBot RiasBot => _riasBot;

        public Credentials Credentials => _credentials;

        public Localization Localization => _localization;

        public RiasDbContext DbContext => _dbContext;

        /// <summary>
        /// Send a confirmation message with or without arguments. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        public Task<DiscordMessage> ReplyConfirmationAsync(string key, params object[] args)
        {
            return Context.Channel.SendConfirmationMessageAsync(_localization.GetText(Context.Guild?.Id, key, args));
        }

        /// <summary>
        /// Send an error message with or without arguments. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public Task<DiscordMessage> ReplyErrorAsync(string key, params object[] args)
        {
            return Context.Channel.SendErrorMessageAsync(_localization.GetText(Context.Guild?.Id, key, args));
        }

        public async Task<DiscordMessage> ReplyAsync(DiscordEmbedBuilder embed)
            => await Context.Channel.SendMessageAsync(embed);

        public async Task SendPaginatedMessageAsync<T>(List<T> items, int itemsPerPage, Func<IEnumerable<T>, int, DiscordEmbedBuilder> embedFunc)
        {
            var pageCount = (items.Count - 1) / itemsPerPage + 1;
            
            var pages = items.Split(itemsPerPage).Select((x, i) =>
            {
                var embed = embedFunc(x, itemsPerPage * i);
                var footerText = GetText(Localization.CommonMenuPage, i + 1, pageCount);
                if (embed.Footer != null)
                    footerText += $" | {embed.Footer.Text}";
                embed.WithFooter(footerText);
                return new Page(embed: embed);
            });

            await Context.Interactivity.SendPaginatedMessageAsync(Context.Channel, Context.User, pages);
        }

        public Task<InteractivityResult<DiscordMessage>> NextMessageAsync()
            => Context.Interactivity.WaitForMessageAsync(x => x.Author.Id == Context.User.Id);

        /// <summary>
        /// Get a translation text with or without arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public string GetText(string key, params object[] args)
        {
            return _localization.GetText(Context.Guild?.Id, key, args);
        }
        
        /// <summary>
        /// Run a task in an async way.
        /// </summary>
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

    public abstract class RiasModule<TService> : RiasModule
        where TService : RiasService
    {
        private readonly TService _service;

        public RiasModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _service = serviceProvider.GetRequiredService<TService>();
        }

        public TService Service => _service;
    }
}