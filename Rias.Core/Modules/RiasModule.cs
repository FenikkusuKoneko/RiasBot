using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Database;
using Rias.Core.Implementation;
using Rias.Core.Services;
using RiasBot.Extensions;

namespace Rias.Core.Modules
{
    public abstract class RiasModule : ModuleBase<RiasCommandContext>, IDisposable
    {
        public Credentials Creds { get; set; }
        public Translations Translations { get; set; }
        public RiasDbContext Db { get; set; }

        public string ParentModuleName => Context.Command.Module.Parent?.Name ?? Context.Command.Module.Name;
        public string LowerParentModuleName => ParentModuleName.ToLower();

        /// <summary>
        /// Send a confirmation message. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        protected async Task<IUserMessage> ReplyConfirmationAsync(string key)
            => await Context.Channel.SendConfirmationMessageAsync(Translations.GetText(Context.Guild?.Id, LowerParentModuleName, key));

        /// <summary>
        /// Send a confirmation message with arguments. The form is an embed with the confirm color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module name of this class.
        /// </summary>
        protected async Task<IUserMessage> ReplyConfirmationAsync(string key, params object[] args)
            => await Context.Channel.SendConfirmationMessageAsync(Translations.GetText(Context.Guild?.Id, LowerParentModuleName, key, args));

        /// <summary>
        /// Send an error message. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        protected async Task<IUserMessage> ReplyErrorAsync(string key)
            => await Context.Channel.SendErrorMessageAsync(Translations.GetText(Context.Guild?.Id, LowerParentModuleName, key));

        /// <summary>
        /// Send an error message with arguments. The form is an embed with the error color.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        protected async Task<IUserMessage> ReplyErrorAsync(string key, params object[] args)
            => await Context.Channel.SendErrorMessageAsync(Translations.GetText(Context.Guild?.Id, LowerParentModuleName, key, args));

        protected async Task<IUserMessage> ReplyAsync(EmbedBuilder embed)
            => await Context.Channel.SendMessageAsync(embed);

        /// <summary>
        /// Get a translation text.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        protected string GetText(string key)
        {
            return Translations.GetText(Context.Guild?.Id, LowerParentModuleName, key);
        }

        /// <summary>
        /// Get a translation text with arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is the lower module type of this class.
        /// </summary>
        protected string GetText(string key, params object[] args)
        {
            return Translations.GetText(Context.Guild?.Id, LowerParentModuleName, key, args);
        }

        protected string GetPrefix()
        {
            return Context.Guild is null ? Creds.Prefix : Db.Guilds.FirstOrDefault(g => g.GuildId == Context.Guild.Id)?.Prefix;
        }

        public void Dispose()
        {
            Db?.Dispose();
        }
    }

    public abstract class RiasModule<TService> : RiasModule where TService : RiasService
    {
        public TService Service { get; set; }
    }
}