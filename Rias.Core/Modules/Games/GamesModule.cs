using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Disqord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Games
{
    [Name("Games")]
    public class GamesModule : RiasModule
    {
        public GamesModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        [Command("rps"), Context(ContextType.Guild)]
        public async Task RpsAsync(string value)
        {
            if (!Enum.TryParse<Rps>(value, true, out var userRps)) return;

            var random = new Random();
            var botRps = (Rps) random.Next(1, 4);
            var botRpsString = botRps.ToString().ToLower();

            if ((int) botRps % 3 + 1 == (int) userRps)
            {
                await ReplyConfirmationAsync(Localization.GamesRpsWon, botRpsString);
            }
            else if ((int) userRps % 3 + 1 == (int) botRps)
            {
                await ReplyErrorAsync(Localization.GamesRpsLost, botRpsString);
            }
            else
            {
                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.Yellow,
                    Description = GetText(Localization.GamesRpsDraw, botRpsString)
                };
                await ReplyAsync(embed);
            }
        }

        [Command("8ball"), Context(ContextType.Guild)]
        public async Task EightBallAsync([Remainder] string _)
        {
            var number = new Random().Next(20);
            await ReplyConfirmationAsync(Localization.GamesEightBallAnswer(number + 1));
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum Rps
        {
            Rock = 1,
            Paper = 2,
            Scissors = 3
        }
    }
}