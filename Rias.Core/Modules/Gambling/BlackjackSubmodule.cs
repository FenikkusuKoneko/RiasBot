using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Gambling
{
    public partial class GamblingModule
    {
        [Name("Blackjack")]
        public class BlackjackSubmodule : RiasModule<BlackjackService>
        {
            private readonly GamblingService _gamblingService;
            
            public BlackjackSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                _gamblingService = serviceProvider.GetRequiredService<GamblingService>();
            }
            
            [Command, Context(ContextType.Guild)]
            public async Task BlackjackAsync(int bet)
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

                var currency = await _gamblingService.GetUserCurrencyAsync(Context.User.Id);
                if (currency < bet)
                {
                    await ReplyErrorAsync(Localization.GamblingCurrencyNotEnough, Credentials.Currency);
                    return;
                }

                if (Service.TryGetBlackjack(Context.User.Id, out _))
                {
                    await ReplyErrorAsync(Localization.GamblingBlackjackSession, Context.Prefix);
                    return;
                }
                
                await Service.CreateBlackjackAsync((DiscordMember) Context.User, Context.Channel, bet);
            }
            
            [Command("resume"), Context(ContextType.Guild)]
            public async Task BlackjackResumeAsync()
            {
                if (!Service.TryGetBlackjack(Context.User.Id, out var blackjack))
                {
                    await ReplyErrorAsync(Localization.GamblingBlackjackNoSession);
                    return;
                }

                await blackjack!.ResendMessageAsync(Context.Channel);
            }
            
            [Command("stop"), Context(ContextType.Guild)]
            public async Task BlackjackStopAsync()
            {
                if (!Service.TryRemoveBlackjack(Context.User.Id, out _))
                {
                    await ReplyErrorAsync(Localization.GamblingBlackjackNoSession);
                    return;
                }
                
                await ReplyConfirmationAsync(Localization.GamblingBlackjackStopped);
            }
        }
    }
}