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
            if(bet < 20)
            {
                await ReplyAsync($"{Context.User.Mention} you can't bet less than 20 {RiasBot.currency}");
                return;
            }

            string[] coins = { "h", "t", "heads", "tails" };
            if (coin != coins[0] && coin != coins[1] && coin != coins[2] && coin != coins[3])
                return;

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
                            userDb.Currency += win - bet;
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
            if (bet < 50)
            {
                await ReplyAsync($"{Context.User.Mention} you can't bet less than 50 {RiasBot.currency}");
                return;
            }

            string[] arrow = { "⬆", "↗", "➡", "↘", "⬇", "↙", "⬅", "↖" };
            float[] wheelMultiple = { 0.0f, 0.3f, 1.3f, 1.5f, 2.0f, 0.5f, 0.1f, 1.7f };
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

                        var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                        embed.WithTitle($"{Context.User} you won {win} {RiasBot.currency}");
                        embed.WithDescription($"「1.7x」\t「0.0x」\t「0.3x」\n\n「0.1x」\t    {arrow[wheel]}    \t「1.3x」\n\n「0.5x」\t「2.0x」\t「1.5x」");
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
