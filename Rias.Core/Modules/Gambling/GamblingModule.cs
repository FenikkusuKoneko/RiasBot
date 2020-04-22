using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Gambling
{
    public class GamblingModule : RiasModule<GamblingService>
    {
        public GamblingModule(IServiceProvider services) : base(services)
        {
        }
        
        [Command("wheel"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task WheelAsync(int bet)
        {
            if (bet < GamblingService.MinimumBet)
            {
                await ReplyErrorAsync(Localization.GamblingBetLessThan, GamblingService.MinimumBet, Credentials.Currency);
                return;
            }

            if (bet > GamblingService.MaximumBet)
            {
                await ReplyErrorAsync(Localization.GamblingBetMoreThan, GamblingService.MaximumBet, Credentials.Currency);
                return;
            }

            var currency = Service.GetUserCurrency(Context.User.Id);
            if (currency < bet)
            {
                await ReplyErrorAsync(Localization.GamblingCurrencyNotEnough, Credentials.Currency);
                return;
            }

            var random = new Random();
            var position = random.Next(8);
            var arrow = Service.Arrows[position];
            var multiplier = Service.Multipliers[position];

            var win = (int) (bet * multiplier) - bet;
            await Service.AddUserCurrencyAsync(Context.User.Id, win);

            var winString = win >= 0
                ? GetText(Localization.GamblingYouWon, win, Credentials.Currency)
                : GetText(Localization.GamblingYouLost, Math.Abs(win), Credentials.Currency);
            
            var embed = new LocalEmbedBuilder()
            {
                Color = win >= 0 ? RiasUtilities.Green : RiasUtilities.Red,
                Title = $"{Context.User} {winString}",
                Description = $"「1.5x」\t「1.7x」\t「2.0x」\n\n「0.2x」\t    {arrow}    \t「1.2x」\n\n「0.0x」\t「0.3x」\t「0.5x」"
            };

            await ReplyAsync(embed);
        }
    }
}