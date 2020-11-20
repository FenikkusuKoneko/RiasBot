using System;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Rias.Implementation
{
    public class RiasCommandContext : CommandContext
    {
        /// <summary>
        /// Gets the logged-in client.
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
        /// Gets the user that executed the command. If the command was executed in a guild, then this will contain the command's member.
        /// </summary>
        public DiscordUser User { get; set; }

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
        
        public RiasCommandContext(IServiceProvider serviceProvider, DiscordMessage message, string prefix)
            : base(serviceProvider)
        {
            var bot = serviceProvider.GetRequiredService<RiasBot>();
            Guild = message.Channel.Guild;

            Client = Guild is not null
                ? bot.Client.ShardClients.FirstOrDefault(x => x.Value.Guilds.ContainsKey(Guild.Id)).Value
                : bot.Client.ShardClients[0];
            
            Channel = message.Channel;
            User = message.Author;
            Message = message;
            Prefix = prefix;
            Interactivity = Client.GetInteractivity();
        }
    }
}