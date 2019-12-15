using System;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Searches
{
    public partial class Searches
    {
        [Name("Cute Girls")]
        public class CuteGirls : RiasModule<CuteGirlsService>
        {
            public CuteGirls(IServiceProvider services) : base(services)
            {
            }

            [Command("neko"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task NekoAsync()
            {
                var embed = new EmbedBuilder
                {
                    Title = GetText("Neko"),
                    Color = RiasUtils.ConfirmColor,
                    ImageUrl = await Service.GetNekoImageAsync(),
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{GetText("PoweredBy")} riasbot.me"
                    }
                };

                await ReplyAsync(embed);
            }
            
            [Command("kitsune"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task KitsuneAsync()
            {
                var embed = new EmbedBuilder
                {
                    Title = GetText("Kitsune"),
                    Color = RiasUtils.ConfirmColor,
                    ImageUrl = await Service.GetNekoImageAsync(),
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{GetText("PoweredBy")} riasbot.me"
                    }
                };

                await ReplyAsync(embed);
            }
        }
    }
}