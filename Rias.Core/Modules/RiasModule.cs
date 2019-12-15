using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Database;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Serilog;

namespace Rias.Core.Modules
{
    public abstract class RiasModule : ModuleBase<RiasCommandContext>
    {
        public readonly Credentials Creds;
        public readonly Resources Resources;

        private readonly IServiceProvider _services;

        public string ParentModuleName => Context.Command.Module.Parent?.Name ?? Context.Command.Module.Name;

        public RiasModule(IServiceProvider services)
        {
            Creds = services.GetRequiredService<Credentials>();
            Resources = services.GetRequiredService<Resources>();
            _services = services;
        }

        /// <summary>
        /// Send a confirmation message with or without arguments. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        public Task<IUserMessage> ReplyConfirmationAsync(string key, params object[] args)
        {
            SplitPrefixKey(out var prefix, ref key);
            return Context.Channel.SendConfirmationMessageAsync(Resources.GetText(Context.Guild?.Id, prefix, key, args));
        }

        /// <summary>
        /// Send an error message with or without arguments. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public Task<IUserMessage> ReplyErrorAsync(string key, params object[] args)
        {
            SplitPrefixKey(out var prefix, ref key);
            return Context.Channel.SendErrorMessageAsync(Resources.GetText(Context.Guild?.Id, prefix, key, args));
        }

        public async Task<IUserMessage> ReplyAsync(EmbedBuilder embed)
            => await Context.Channel.SendMessageAsync(embed);

        /// <summary>
        /// Get a translation text with or without arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public string GetText(string key, params object[] args)
        {
            SplitPrefixKey(out var prefix, ref key);
            return Resources.GetText(Context.Guild?.Id, prefix, key, args);
        }

        public string GetPrefix()
        {
            var guild = Context.Guild;
            if (guild == null) return Creds.Prefix;
            using var db = _services.GetRequiredService<RiasDbContext>();
            var prefix = db.Guilds.FirstOrDefault(g => g.GuildId == guild.Id)?.Prefix;
            return !string.IsNullOrEmpty(prefix) ? prefix : Creds.Prefix;
        }

        /// <summary>
        /// Run a task in an async way.
        /// </summary>
        /// <param name="func"></param>
        public Task RunTaskAsync(Task func)
        {
            _ = Task.Run(async () =>
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

        private void SplitPrefixKey(out string prefix, ref string key)
        {
            if (!key.StartsWith('#'))
            {
                prefix = ParentModuleName;
                return;
            }

            var index = key.IndexOf('_');

            if (index == -1)
                throw new IndexOutOfRangeException($"The key {key} contains a '#' but not a '_', the prefix cannot be extracted");
            
            prefix = key[1..index];
            key = key[++index..];
        }
    }

    public abstract class RiasModule<TService> : RiasModule where TService : RiasService
    {
        public readonly TService Service;

        public RiasModule(IServiceProvider services) : base(services)
        {
            Service = services.GetRequiredService<TService>();
        }
    }
}