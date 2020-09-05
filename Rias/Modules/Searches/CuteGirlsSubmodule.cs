using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Qmmands;
using Rias.Commons;
using Rias.Implementation;

namespace Rias.Modules.Searches
{
    public partial class SearchesModule
    {
        [Name("Cute Girls")]
        public class CuteGirlsSubmodule : RiasModule
        {
            private readonly HttpClient _httpClient;
            
            public CuteGirlsSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }
            
            [Command("neko"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task NekoAsync()
            {
                using var response = await _httpClient.GetAsync("https://riasbot.me/api/neko");
                var nekoImage = response.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())["url"]
                    : null;
                
                var embed = new DiscordEmbedBuilder
                {
                    Title = GetText(Localization.SearchesNeko),
                    Color = RiasUtilities.ConfirmColor,
                    ImageUrl = nekoImage,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{GetText(Localization.ReactionsPoweredBy)} riasbot.me"
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
                
                var embed = new DiscordEmbedBuilder
                {
                    Title = GetText(Localization.SearchesKitsune),
                    Color = RiasUtilities.ConfirmColor,
                    ImageUrl = kitsuneImage,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{GetText(Localization.ReactionsPoweredBy)} riasbot.me"
                    }
                };

                await ReplyAsync(embed);
            }
        }
    }
}