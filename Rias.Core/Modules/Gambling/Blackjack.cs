using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Services;

namespace Rias.Core.Modules.Gambling
{
    public partial class Gambling
    {
        [Name("Blackjack")]
        public class Blackjack : RiasModule<BlackjackService>
        {
            private readonly GamblingService _gamblingService;

            public Blackjack(IServiceProvider services) : base(services)
            {
                _gamblingService = services.GetRequiredService<GamblingService>();
            }
            
            [Command, Context(ContextType.Guild)]
            public async Task BlackjackAsync(int bet)
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

                var currency = _gamblingService.GetUserCurrency(Context.User);
                if (currency < bet)
                {
                    await ReplyErrorAsync("CurrencyNotEnough", Creds.Currency);
                    return;
                }

                if (Service.TryGetBlackjack((SocketGuildUser) Context.User, out _))
                {
                    await ReplyErrorAsync("BlackjackSession", GetPrefix());
                    return;
                }
                
                await Service.CreateBlackjackAsync((SocketGuildUser) Context.User, Context.Channel, bet);
            }

            [Command("resume"), Context(ContextType.Guild)]
            public async Task BlackjackResumeAsync()
            {
                if (!Service.TryGetBlackjack((SocketGuildUser) Context.User, out var blackjack))
                {
                    await ReplyErrorAsync("BlackjackNoSession");
                    return;
                }

                await blackjack!.ResendMessageAsync(Context.Channel);
            }
            
            [Command("stop"), Context(ContextType.Guild)]
            public async Task BlackjackStopAsync()
            {
                if (!Service.TryRemoveBlackjack((SocketGuildUser) Context.User, out _))
                {
                    await ReplyErrorAsync("BlackjackNoSession");
                    return;
                }

                await ReplyConfirmationAsync("BlackjackStopped");
            }
        }
    }
}