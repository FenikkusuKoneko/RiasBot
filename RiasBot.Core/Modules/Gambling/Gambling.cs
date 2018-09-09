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

namespace RiasBot.Modules.Gambling
{
    public partial class Gambling : RiasModule
    {
        private readonly CommandHandler _ch;
        private readonly CommandService _service;
        private readonly DbService _db;

        public Gambling(CommandHandler ch, CommandService service, DbService db)
        {
            _ch = ch;
            _service = service;
            _db = db;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task BetRoll(int bet)
        {
            if(bet < 50)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you can't bet less than 50 {RiasBot.Currency}");
                return;
            }

            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            var roll = rnd.Next(100) + 1; //heads or tails

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(Xp => Xp.UserId == Context.User.Id).FirstOrDefault();
                try
                {
                    if (bet <= userDb.Currency)
                    {
                        var win = 0;
                        float multiplier = 0;
                        if (roll >= 99)
                        {
                            win += bet * 5;
                            multiplier = 5;
                        }
                        else if (roll >= 95)
                        {
                            win += bet * 2;
                            multiplier = 2;
                        }
                        else if (roll >= 90)
                        {
                            win += (int)(bet * 1.5f);
                            multiplier = 1.5f;
                        }
                        else if (roll >= 80)
                        {
                            win += bet;
                            multiplier = 1;
                        }
                        if (win > 0)
                        {
                            userDb.Currency += win - bet;
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User} you rolled {roll} {Format.Bold($"(x{multiplier})")}. You won {win}{RiasBot.Currency}");
                        }
                        else
                        {
                            userDb.Currency += win - bet;
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User} you rolled {roll} {Format.Bold("(x0)")}.");
                        }
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have enough {RiasBot.Currency}");
                    }
                }
                catch { }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Wheel(int bet)
        {
            if (bet < 50)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you can't bet less than 50 {RiasBot.Currency}");
                return;
            }

            string[] arrow = { "⬆", "↗", "➡", "↘", "⬇", "↙", "⬅", "↖" };
            float[] wheelMultiple = { 1.7f, 2.0f, 1.2f, 0.5f, 0.3f, 0.0f, 0.2f, 1.5f };
            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            var wheel = rnd.Next(8);

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(Xp => Xp.UserId == Context.User.Id).FirstOrDefault();
                try
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
                        await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have enough {RiasBot.Currency}");
                    }
                }
                catch
                {

                }
            }
        }
    }
}
