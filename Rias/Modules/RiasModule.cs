using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
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
        
        public RiasModule(IServiceProvider serviceProvider)
        {
            RiasBot = serviceProvider.GetRequiredService<RiasBot>();
            Configuration = serviceProvider.GetRequiredService<Configuration>();
            Localization = serviceProvider.GetRequiredService<Localization>();

            _scope = serviceProvider.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        }

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
            => Localization.GetText(Context.Guild?.Id, key, args);
        
        /// <summary>
        /// Run a task in an async way.
        /// </summary>
        public Task RunTaskAsync(Task task)
        {
            Task.Run(async () =>
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception thrown in a command");
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