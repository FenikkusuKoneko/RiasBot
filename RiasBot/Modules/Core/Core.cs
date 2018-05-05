using Discord;
using Discord.Commands;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Core
{
    public partial class Core : RiasModule
    {
        public readonly CommandHandler _ch;
        public readonly DbService _db;

        public Core(CommandHandler ch, DbService db)
        {
            _ch = ch;
            _db = db;
        }

        [Command("cchincat")]
        public async Task TakeChannels([Remainder]string categoryChannel)
        {
            var catCh = categoryChannel.Split("->");
            string category = catCh[0].TrimEnd();
            string channel = catCh[1].TrimStart();

            var catChannel = (await Context.Guild.GetCategoriesAsync()).Where(x => x.Name.ToUpperInvariant() == category.ToUpperInvariant()).FirstOrDefault();

            if (channel.Length < 2 || channel.Length > 100)
            {
                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length of the channel must be between 2 and 100 characters");
                return;
            }
            var ch = await Context.Guild.CreateTextChannelAsync(channel).ConfigureAwait(false);
            await ch.ModifyAsync(x => x.CategoryId = catChannel.Id).ConfigureAwait(false);
            await Context.Channel.SendConfirmationEmbed($"Category {Format.Bold(ch.Name)} was created in category {Format.Bold(catChannel.Name)} successfully.");
        }
    }
}
