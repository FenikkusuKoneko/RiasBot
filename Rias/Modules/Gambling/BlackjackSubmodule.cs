using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Gambling
{
    public partial class GamblingModule
    {
        [Name("Blackjack")]
        [Group("blackjack", "bj")]
        public class BlackjackSubmodule : RiasModule<BlackjackService>
        {
            private readonly GamblingService _gamblingService;
            
            public BlackjackSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _gamblingService = serviceProvider.GetRequiredService<GamblingService>();
            }

            [Command]
            [Context(ContextType.Guild)]
            public async Task BlackjackAsync(int bet)
            {
                if (bet < GamblingService.MinimumBet)
                {
                    await ReplyErrorAsync(Localization.GamblingBetLessThan, GamblingService.MinimumBet, Configuration.Currency);
                    return;
                }

                if (bet > GamblingService.MaximumBet)
                {
                    await ReplyErrorAsync(Localization.GamblingBetMoreThan, GamblingService.MaximumBet, Configuration.Currency);
                    return;
                }

                var currency = await _gamblingService.GetUserCurrencyAsync(Context.User.Id);
                if (currency < bet)
                {
                    await ReplyErrorAsync(Localization.GamblingCurrencyNotEnough, Configuration.Currency);
                    return;
                }

                await Service.PlayBlackjackAsync((DiscordMember) Context.User, Context.Channel, bet, Context.Prefix);
            }

            [Command("resume")]
            [Context(ContextType.Guild)]
            public Task BlackjackResumeAsync()
                => Service.ResumeBlackjackAsync((DiscordMember) Context.User, Context.Channel);

            [Command("stop")]
            [Context(ContextType.Guild)]
            public Task BlackjackStopAsync()
                => Service.StopBlackjackAsync((DiscordMember) Context.User, Context.Channel);
        }
    }
}