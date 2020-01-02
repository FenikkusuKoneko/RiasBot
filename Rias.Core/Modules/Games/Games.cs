using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Games
{
    [Name("Games")]
    public class Games : RiasModule
    {
        public Games(IServiceProvider services) : base(services)
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
                await ReplyConfirmationAsync("RpsWon", botRpsString);
            }
            else if ((int) userRps % 3 + 1 == (int) botRps)
            {
                await ReplyErrorAsync("RpsLost", botRpsString);
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.Yellow,
                    Description = GetText("RpsDraw", botRpsString)
                };
                await ReplyAsync(embed);
            }
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