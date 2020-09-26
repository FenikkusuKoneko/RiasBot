using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Commons;
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
        public readonly RiasBot RiasBot;
        public readonly Credentials Credentials;
        public readonly Localization Localization;
        public readonly RiasDbContext DbContext;
        
        private readonly IServiceScope _scope;
        
        public RiasModule(IServiceProvider serviceProvider)
        {
            RiasBot = serviceProvider.GetRequiredService<RiasBot>();
            Credentials = serviceProvider.GetRequiredService<Credentials>();
            Localization = serviceProvider.GetRequiredService<Localization>();

            _scope = serviceProvider.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        }

        protected override async ValueTask BeforeExecutedAsync()
        {
            if (Context.Channel.Type != ChannelType.Text)
                return;
            
            var channelPermissions = Context.Channel.Guild.CurrentMember.PermissionsIn(Context.Channel);
            var channelEmbedPerm = channelPermissions.HasPermission(Permissions.EmbedLinks);
            var serverEmbedPerm = Context.Channel.Guild.CurrentMember.GetPermissions().HasPermission(Permissions.EmbedLinks);
            
            if (!serverEmbedPerm && !channelEmbedPerm)
            {
                await Context.Channel.SendMessageAsync(GetText(Localization.ServiceNoEmbedLinksPermission));
                throw new CommandNoPermissionsException("No embed links permission.");
            }

            if (serverEmbedPerm && !channelEmbedPerm)
            {
                await Context.Channel.SendMessageAsync(GetText(Localization.ServiceNoEmbedLinksChannelPermission));
                throw new CommandNoPermissionsException("No embed links permission.");
            }
        }

        /// <summary>
        /// Send a confirmation message with or without arguments. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        public Task<DiscordMessage> ReplyConfirmationAsync(string key, params object[] args)
        {
            return Context.Channel.SendConfirmationMessageAsync(Localization.GetText(Context.Guild?.Id, key, args));
        }

        /// <summary>
        /// Send an error message with or without arguments. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public Task<DiscordMessage> ReplyErrorAsync(string key, params object[] args)
        {
            return Context.Channel.SendErrorMessageAsync(Localization.GetText(Context.Guild?.Id, key, args));
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
            return Localization.GetText(Context.Guild?.Id, key, args);
        }
        
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
        public readonly TService Service;

        public RiasModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Service = serviceProvider.GetRequiredService<TService>();
        }
    }
}