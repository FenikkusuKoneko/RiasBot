using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiasBot.Extensions;
using RiasBot.Modules.Gambling.Services;

namespace RiasBot.Modules.Gambling
{
    public partial class Gambling : RiasModule
    {
        private readonly CommandHandler _ch;
        private readonly DbService _db;
        private readonly BlackjackService _blackjackService;

        public Gambling(CommandHandler ch, DbService db, BlackjackService blackjackService)
        {
            _ch = ch;
            _db = db;
            _blackjackService = blackjackService;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Wheel(int bet)
        {
            if (bet < 5)
            {
                await Context.Channel.SendErrorMessageAsync($"You cannot bet less than 5 {RiasBot.Currency}.");
            }
            else if (bet > 1000)
            {
                await Context.Channel.SendErrorMessageAsync($"You cannot bet more than 1000 {RiasBot.Currency}.");
            }
            else
            {
                string[] arrow = { "⬆", "↗", "➡", "↘", "⬇", "↙", "⬅", "↖" };
                float[] wheelMultiple = { 1.7f, 2.0f, 1.2f, 0.5f, 0.3f, 0.0f, 0.2f, 1.5f };
                var rnd = new Random((int)DateTime.UtcNow.Ticks);
                var wheel = rnd.Next(8);

                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == Context.User.Id);
                    if (userDb != null)
                    {
                        if (bet <= userDb.Currency)
                        {
                            var win = (int)(bet * wheelMultiple[wheel]);
                            userDb.Currency += win - bet;
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                            embed.WithTitle($"{Context.User} you won {win} {RiasBot.Currency}");
                            embed.WithDescription($"「1.5x」\t「1.7x」\t「2.0x」\n\n「0.2x」\t    {arrow[wheel]}    \t「1.2x」\n\n「0.0x」\t「0.3x」\t「0.5x」");
                            await Context.Channel.SendMessageAsync("", embed: embed.Build());
                        }
                        else
                        {
                            await Context.Channel.SendErrorMessageAsync($"You don't have enough {RiasBot.Currency}.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync($"You don't have enough {RiasBot.Currency}.");
                    }
                }
            }
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task BlackjackAsync(int bet)
        {
            var currency = _blackjackService.GetCurrency((IGuildUser)Context.User);
            if (bet < 5)
            {
                await Context.Channel.SendErrorMessageAsync($"You cannot bet less than 5 {RiasBot.Currency}.").ConfigureAwait(false);
            }
            else if (bet > 1000)
            {
                await Context.Channel.SendErrorMessageAsync($"You cannot bet more than 1000 {RiasBot.Currency}.").ConfigureAwait(false);
            }
            else if (bet <= currency)
            {
                var bj = _blackjackService.GetGame((IGuildUser) Context.User);
                if (bj is null)
                {
                    bj = _blackjackService.GetOrCreateGame((IGuildUser) Context.User);
                    await bj.InitializeGameAsync(Context.Channel, (IGuildUser)Context.User, bet).ConfigureAwait(false); 
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync($"You are already in a blackjack session! Type `{_ch.Prefix}bj resume` to continue the session in this channel.").ConfigureAwait(false);
                } 
            }
            else
            {
                await Context.Channel.SendErrorMessageAsync($"You don't have enough {RiasBot.Currency}.").ConfigureAwait(false);
            }
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task BlackjackAsync(string subcommand)
        {
            subcommand = subcommand.ToLowerInvariant();
            var bj = _blackjackService.GetGame((IGuildUser) Context.User);
            switch (subcommand)
            {
                case "resume":
                    if (bj != null)
                        await bj.ResumeGameAsync((IGuildUser) Context.User, Context.Channel).ConfigureAwait(false);
                    else
                        await Context.Channel.SendErrorMessageAsync("You are not in a blackjack session!").ConfigureAwait(false);
                    break;
                case "stop":
                case "surrender":
                    if (bj != null)
                    {
                        await bj.StopGameAsync((IGuildUser)Context.User).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync("Blackjack stopped!").ConfigureAwait(false);
                    }
                    else
                        await Context.Channel.SendErrorMessageAsync("You are not in a blackjack session!").ConfigureAwait(false);
                    break;
            }
        }
    }
}
