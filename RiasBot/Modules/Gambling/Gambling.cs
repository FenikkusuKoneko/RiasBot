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
        public async Task BetFlip(int bet, string coin)
        {
            if(bet < 2)
            {
                await ReplyAsync($"{Context.User.Mention} you can't bet less than 2 {RiasBot.currency}");
                return;
            }

            string[] coins = { "h", "t", "heads", "tails" };
            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            int hot = rnd.Next(2); //heads or tails

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(Xp => Xp.UserId == Context.User.Id).FirstOrDefault();
                try
                {
                    if (bet <= userDb.Currency)
                    {
                        if (coins[hot] == coin || coins[hot + 2] == coin)
                        {
                            int win = (int)(bet * 1.95);
                            userDb.Currency += win;
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            await ReplyAsync($"{Context.User.Mention} you guessed it ^^. You won {win} {RiasBot.currency}");
                        }
                        else
                        {
                            userDb.Currency -= bet;
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            await ReplyAsync($"{Context.User.Mention} you didn't guess it. You lost {bet} {RiasBot.currency}");
                        }
                    }
                    else
                    {
                        await ReplyAsync($"{Context.User.Mention} you don't have enough {RiasBot.currency}");
                    }
                }
                catch { }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Wheel(int bet)
        {
            if (bet < 10)
            {
                await ReplyAsync($"{Context.User.Mention} you can't bet less than 10 {RiasBot.currency}");
                return;
            }

            string[] arrow = { "⬆", "↗", "➡", "↘", "⬇", "↙", "⬅", "↖" };
            float[] wheelMultiple = { 2.3f, 0.1f, 0.2f, 0.3f, 0.5f, 1.2f, 1.5f, 1.7f };
            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            int wheel = rnd.Next(8);

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(Xp => Xp.UserId == Context.User.Id).FirstOrDefault();
                try
                {
                    if (bet <= userDb.Currency)
                    {
                        int win = (int)(bet * wheelMultiple[wheel]);
                        userDb.Currency += win - bet;
                        await db.SaveChangesAsync().ConfigureAwait(false);

                        var embed = new EmbedBuilder().WithColor(RiasBot.color);
                        embed.WithTitle($"{Context.User} you won {win} {RiasBot.currency}");
                        embed.WithDescription($"「1.7x」\t「2.3x」\t「0.1x」\n\n「1.5x」\t    {arrow[wheel]}    \t「0.2x」\n\n「1.2x」\t「0.3x」\t「0.5x」");
                        await ReplyAsync("", embed: embed.Build());
                    }
                    else
                    {
                        await ReplyAsync($"{Context.User.Mention} you don't have enough {RiasBot.currency}");
                    }
                }
                catch
                {

                }
            }
        }
    }
}
