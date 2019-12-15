using System;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Gambling
{
    [Name("Gambling")]
    public partial class Gambling : RiasModule<GamblingService>
    {
        public Gambling(IServiceProvider services) : base(services) {}

        [Command("wheel"), Context(ContextType.Guild),
         Cooldown(3, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task WheelAsync(int bet)
        {
            if (bet < GamblingService.MinimumBet)
            {
                await ReplyErrorAsync("BetLessThan", GamblingService.MinimumBet, Creds.Currency);
                return;
            }

            if (bet > GamblingService.MaximumBet)
            {
                await ReplyErrorAsync("BetMoreThan", GamblingService.MaximumBet, Creds.Currency);
                return;
            }

            var currency = Service.GetUserCurrency(Context.User);
            if (currency < bet)
            {
                await ReplyErrorAsync("CurrencyNotEnough", Creds.Currency);
                return;
            }

            var random = new Random();
            var position = random.Next(8);
            var arrow = Service.Arrows[position];
            var multiplier = Service.Multipliers[position];

            var win = (int) (bet * multiplier) - bet;
            await Service.AddUserCurrencyAsync(Context.User.Id, win);

            Color color;
            string winString;
            if (win >= 0)
            {
                winString = GetText("YouWon", win, Creds.Currency);
                color = RiasUtils.Green;
            }
            else
            {
                winString = GetText("YouLost", Math.Abs(win), Creds.Currency);
                color = RiasUtils.Red;
            }
            
            var embed = new EmbedBuilder()
            {
                Color = color,
                Title = $"{Context.User} {winString}",
                Description = $"「1.5x」\t「1.7x」\t「2.0x」\n\n「0.2x」\t    {arrow}    \t「1.2x」\n\n「0.0x」\t「0.3x」\t「0.5x」"
            };

            await ReplyAsync(embed);
        }
    }
}