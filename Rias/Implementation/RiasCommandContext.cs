using System;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Rias.Implementation
{
    public class RiasCommandContext : CommandContext
    {
        private readonly DiscordClient _client = null!;
        private readonly DiscordGuild? _guild;
        private readonly DiscordChannel _channel;
        private readonly DiscordUser _user;
        private readonly DiscordMessage _message;
        private readonly string _prefix;
        private readonly InteractivityExtension _interactivity;
        
        public RiasCommandContext(IServiceProvider serviceProvider, DiscordMessage message, string prefix)
            : base(serviceProvider)
        {
            var bot = serviceProvider.GetRequiredService<RiasBot>();
            _guild = message.Channel.Guild;

            if (_guild is not null)
                _client = bot.Client.ShardClients.FirstOrDefault(x => x.Value.Guilds.ContainsKey(_guild.Id)).Value;

            _client ??= bot.Client.ShardClients[0];
            
            _channel = message.Channel;
            _user = message.Author;
            _message = message;
            _prefix = prefix;
            _interactivity = Client.GetInteractivity();
        }
        
        /// <summary>
        /// Gets the logged-in client.
        /// </summary>
        public DiscordClient Client => _client;

        /// <summary>
        /// Gets the guild where the command was executed, null if context is a DM.
        /// </summary>
        public DiscordGuild? Guild => _guild;

        /// <summary>
        /// Gets the channel where the command was executed.
        /// </summary>
        public DiscordChannel Channel => _channel;

        /// <summary>
        /// Gets the user that executed the command.
        /// </summary>
        public DiscordUser User => _user;

        /// <summary>
        /// Gets the current logged-in user.
        /// </summary>
        public DiscordMember? CurrentMember => _guild?.CurrentMember;

        /// <summary>
        /// Gets the user's message that executed the command.
        /// </summary>
        public DiscordMessage Message => _message;

        /// <summary>
        /// Gets the prefix of the server or the default one.
        /// </summary>
        public string Prefix => _prefix;

        public InteractivityExtension Interactivity => _interactivity;
    }
}