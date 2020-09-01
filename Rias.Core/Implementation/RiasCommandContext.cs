using System;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Rias.Core.Implementation
{
    public class RiasCommandContext : CommandContext
    {
        /// <summary>
        /// Gets the main bot.
        /// </summary>
        public readonly RiasBot RiasBot;

        /// <summary>
        /// Gets the logged-in client;
        /// </summary>
        public readonly DiscordClient Client;
        
        /// <summary>
        /// Gets the guild where the command was executed, null if context is a DM.
        /// </summary>
        public readonly DiscordGuild? Guild;

        /// <summary>
        /// Gets the channel where the command was executed.
        /// </summary>
        public readonly DiscordChannel Channel;

        /// <summary>
        /// Gets the user that executed the command.
        /// </summary>
        public readonly DiscordUser User;

        /// <summary>
        /// Gets the current logged-in user.
        /// </summary>
        public DiscordMember? CurrentMember => Guild?.CurrentMember;

        /// <summary>
        /// Gets the user's message that executed the command.
        /// </summary>
        public readonly DiscordMessage Message;

        /// <summary>
        /// Gets the prefix of the server or the default one.
        /// </summary>
        public readonly string Prefix;

        public readonly InteractivityExtension Interactivity;
        
#pragma warning disable 8618
        public RiasCommandContext(IServiceProvider serviceProvider, DiscordMessage message, string prefix) : base(serviceProvider)
#pragma warning restore 8618
        {
            RiasBot = serviceProvider.GetRequiredService<RiasBot>();
            Guild = message.Channel.Guild;

            if (Guild is not null)
                Client = RiasBot.Client.ShardClients.FirstOrDefault(x => x.Value.Guilds.ContainsKey(Guild.Id)).Value;

            Client ??= RiasBot.Client.ShardClients[0];
            Channel = message.Channel;
            User = message.Author;
            Message = message;
            Prefix = prefix;
            Interactivity = Client.GetInteractivity();
        }
    }
}