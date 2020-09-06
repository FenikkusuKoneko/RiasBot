using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Rias.Configuration;
using Rias.Extensions;
using Rias.Implementation;
using Serilog;

namespace Rias.Services
{
    public class RiasService
    {
        private readonly RiasBot _riasBot;
        private readonly Credentials _credentials;
        private readonly Localization _localization;

        public RiasService(IServiceProvider serviceProvider)
        {
            _riasBot = serviceProvider.GetRequiredService<RiasBot>();
            _credentials = serviceProvider.GetRequiredService<Credentials>();
            _localization = serviceProvider.GetRequiredService<Localization>();
        }
        
        public RiasBot RiasBot => _riasBot;
        
        public Credentials Credentials => _credentials;
        
        public Localization Localization => _localization;

        /// <summary>
        /// Send a confirmation message with arguments. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        public Task<DiscordMessage> ReplyConfirmationAsync(DiscordChannel channel, ulong guildId, string key, params object[] args)
        {
            return channel.SendConfirmationMessageAsync(_localization.GetText(guildId, key, args));
        }

        /// <summary>
        /// Send an error message with arguments. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public Task<DiscordMessage> ReplyErrorAsync(DiscordChannel channel, ulong guildId, string key, params object[] args)
        {
            return channel.SendErrorMessageAsync(_localization.GetText(guildId, key, args));
        }

        /// <summary>
        /// Get a translation text with or without arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        public string GetText(ulong? guildId, string key, params object[] args)
        {
            return _localization.GetText(guildId, key, args);
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
    }
}