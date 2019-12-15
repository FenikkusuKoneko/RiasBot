using System;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Serilog;

namespace Rias.Core.Services
{
    /// <summary>
    /// Each service that implement this class will be added in the <see cref="ServiceCollection"/>.
    /// </summary>
    public abstract class RiasService
    {
        public readonly IServiceProvider Services;
        public readonly Credentials Creds;
        public readonly Resources Resources;

        protected RiasService(IServiceProvider services)
        {
            Services = services;
            Creds = services.GetRequiredService<Credentials>();
            Resources = services.GetRequiredService<Resources>();
        }

        /// <summary>
        /// Send a confirmation message with arguments. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        public Task<IUserMessage> ReplyConfirmationAsync(IMessageChannel channel, ulong guildId, string? prefix, string key, params object[] args)
        {
            SplitPrefixKey(ref prefix, ref key);
            return channel.SendConfirmationMessageAsync(Resources.GetText(guildId, prefix, key, args));
        }

        /// <summary>
        /// Send an error message with arguments. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public Task<IUserMessage> ReplyErrorAsync(IMessageChannel channel, ulong guildId, string? prefix, string key, params object[] args)
        {
            SplitPrefixKey(ref prefix, ref key);
            return channel.SendErrorMessageAsync(Resources.GetText(guildId, prefix, key, args));
        }

        /// <summary>
        /// Get a translation text with or without arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public string GetText(ulong guildId, string? prefix, string key, params object[] args)
        {
            SplitPrefixKey(ref prefix, ref key);
            return Resources.GetText(guildId, prefix, key, args);
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

        public void SplitPrefixKey(ref string? prefix, ref string key)
        {
            if (!key.StartsWith('#')) return;

            var index = key.IndexOf('_');
            prefix = key[1..index];
            key = key[++index..];
        }
    }
}