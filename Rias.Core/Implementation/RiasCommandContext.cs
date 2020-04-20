using System;
using Disqord;
using Qmmands;

namespace Rias.Core.Implementation
{
    public class RiasCommandContext : CommandContext
    {
        /// <summary>
        /// Gets the guild where the command was executed, null if context is a DM.
        /// </summary>
        public readonly CachedGuild? Guild;

        /// <summary>
        /// Gets the channel where the command was executed.
        /// </summary>
        public readonly ICachedMessageChannel Channel;

        /// <summary>
        /// Gets the user that executed the command.
        /// </summary>
        public readonly CachedUser User;

        /// <summary>
        /// Gets the current logged-in user.
        /// </summary>
        public CachedMember? CurrentMember => Guild?.CurrentMember;

        /// <summary>
        /// Gets the user's message that executed the command.
        /// </summary>
        public readonly CachedUserMessage Message;

        /// <summary>
        /// Gets the prefix of the server or the default one.
        /// </summary>
        public readonly string Prefix;
        
        public RiasCommandContext(CachedUserMessage message, IServiceProvider serviceProvider, string prefix) : base(serviceProvider)
        {
            Guild = (message.Channel as CachedTextChannel)?.Guild;
            Channel = message.Channel;
            User = message.Author;
            Message = message;
            Prefix = prefix;
        }
    }
}