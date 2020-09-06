using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Implementation;

namespace Rias.Modules.Games
{
    [Name("Games")]
    public class GamesModule : RiasModule
    {
        public GamesModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum Rps
        {
            Rock = 1,
            Paper = 2,
            Scissors = 3
        }

        [Command("rps")]
        [Context(ContextType.Guild)]
        public async Task RpsAsync(string value)
        {
            if (!Enum.TryParse<Rps>(value, true, out var userRps)) return;

            var random = new Random();
            var botRps = (Rps)random.Next(1, 4);
            var botRpsString = botRps.ToString().ToLower();

            if ((int)botRps % 3 + 1 == (int)userRps)
            {
                await ReplyConfirmationAsync(Localization.GamesRpsWon, botRpsString);
            }
            else if ((int)userRps % 3 + 1 == (int)botRps)
            {
                await ReplyErrorAsync(Localization.GamesRpsLost, botRpsString);
            }
            else
            {
                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.Yellow,
                    Description = GetText(Localization.GamesRpsDraw, botRpsString)
                };
                await ReplyAsync(embed);
            }
        }

        [Command("8ball")]
        [Context(ContextType.Guild)]
        public async Task EightBallAsync([Remainder] string message)
        {
            var number = new Random().Next(20);
            await ReplyConfirmationAsync(Localization.GamesEightBallAnswer(number + 1));
        }
    }
}