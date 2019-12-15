using System;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Games
{
    [Name("Games")]
    public class Games : RiasModule<GamesService>
    {
        public Games(IServiceProvider services) : base(services) {}

        [Command("rps"), Context(ContextType.Guild)]
        public async Task RpsAsync(string value)
        {
            if (!Enum.TryParse<GamesService.Rps>(value, true, out var userRps)) return;

            var botRps = Service.Choose();
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
    }
}