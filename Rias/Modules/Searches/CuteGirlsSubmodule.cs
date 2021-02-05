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
            
            public CuteGirlsSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }

            [Command("neko", "catgirl")]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task NekoAsync()
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = GetText(Localization.SearchesNeko),
                    Color = RiasUtilities.ConfirmColor,
                    ImageUrl = await GetImageAsync("neko"),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{GetText(Localization.ReactionsPoweredBy)} rias.gg"
                    }
                };

                await ReplyAsync(embed);
            }

            [Command("kitsune", "foxgirl")]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task KitsuneAsync()
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = GetText(Localization.SearchesKitsune),
                    Color = RiasUtilities.ConfirmColor,
                    ImageUrl = await GetImageAsync("kitsune"),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{GetText(Localization.ReactionsPoweredBy)} rias.gg"
                    }
                };

                await ReplyAsync(embed);
            }
            
            private async Task<string?> GetImageAsync(string type)
            {
                using var response = await _httpClient.GetAsync($"https://rias.gg/api/images?type={type}");
                if (!response.IsSuccessStatusCode)
                    return null;

                return JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())!["url"];
            }
        }
    }
}