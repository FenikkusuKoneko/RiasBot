using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Searches
{
    public partial class Searches
    {
        [Name("Cute Girls")]
        public class CuteGirls : RiasModule
        {
            private readonly HttpClient _httpClient;
            
            public CuteGirls(IServiceProvider services) : base(services)
            {
                _httpClient = services.GetRequiredService<HttpClient>();
            }

            [Command("neko"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task NekoAsync()
            {
                using var response = await _httpClient.GetAsync("https://riasbot.me/api/neko");
                var nekoImage = response.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())["url"]
                    : null;
                
                var embed = new EmbedBuilder
                {
                    Title = GetText("Neko"),
                    Color = RiasUtils.ConfirmColor,
                    ImageUrl = nekoImage,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{GetText("#Reactions_PoweredBy")} riasbot.me"
                    }
                };

                await ReplyAsync(embed);
            }
            
            [Command("kitsune"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task KitsuneAsync()
            {
                using var response = await _httpClient.GetAsync("https://riasbot.me/api/kitsune");
                var kitsuneImage = response.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())["url"]
                    : null;
                
                var embed = new EmbedBuilder
                {
                    Title = GetText("Kitsune"),
                    Color = RiasUtils.ConfirmColor,
                    ImageUrl = kitsuneImage,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{GetText("#Reactions_PoweredBy")} riasbot.me"
                    }
                };

                await ReplyAsync(embed);
            }
        }
    }
}