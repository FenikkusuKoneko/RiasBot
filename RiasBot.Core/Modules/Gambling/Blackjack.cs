using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Gambling.Services;
using RiasBot.Services;

namespace RiasBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class Blackjack : RiasSubmodule<BlackjackService>
        {
            private readonly CommandHandler _ch;

            public Blackjack(CommandHandler ch)
            {
                _ch = ch;
            }
            
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task BlackjackAsync(int bet)
            {
                var currency = _service.GetCurrency((IGuildUser)Context.User);
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
                    var bj = _service.GetGame((IGuildUser) Context.User);
                    if (bj is null)
                    {
                        bj = _service.GetOrCreateGame((IGuildUser) Context.User);
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
                var bj = _service.GetGame((IGuildUser) Context.User);
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
}