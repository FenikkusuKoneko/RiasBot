using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Searches
{
    [Name("Searches")]
    public class SearchesModule : RiasModule
    {
        private readonly HttpClient _httpClient;
        
        public SearchesModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }
        
        [Command("wikipedia"), Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task WikipediaAsync([Remainder] string title)
        {
            await Context.Channel.TriggerTypingAsync();
            using var response = await _httpClient.GetAsync("https://en.wikipedia.org//w/api.php?action=query&format=json&prop=info&redirects=1&formatversion=2&inprop=url&titles=" +
                                                            Uri.EscapeDataString(title));
            if (!response.IsSuccessStatusCode)
            {
                await ReplyErrorAsync(Localization.SearchesNotFound);
                return;
            }

            var result = await response.Content.ReadAsStringAsync();
            var page = JObject.Parse(result)
                .SelectToken("query.pages")?[0];

            if (page is null)
            {
                await ReplyErrorAsync(Localization.SearchesNotFound);
                return;
            }

            if (page.Value<bool>("missing"))
                await ReplyErrorAsync(Localization.SearchesNotFound);
            else
                await Context.Channel.SendMessageAsync(page.Value<string>("fullurl"));
        }
        
        [Command("urbandictionary"), Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task UrbanDictionary([Remainder] string term)
        {
            if (string.IsNullOrEmpty(Credentials.UrbanDictionaryApiKey))
            {
                await ReplyErrorAsync(Localization.SearchesUrbanDictionaryNoApiKey);
                return;
            }
            
            await Context.Channel.TriggerTypingAsync();
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", Credentials.UrbanDictionaryApiKey);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            using var response = await httpClient.GetAsync($"https://mashape-community-urban-dictionary.p.rapidapi.com/define?term={Uri.EscapeUriString(term)}");
            if (!response.IsSuccessStatusCode)
            {
                await ReplyErrorAsync(Localization.SearchesDefinitionNotFound);
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var definitions = JObject.Parse(content)
                .SelectToken("list")?
                .ToObject<List<UrbanDictionaryDefinition>>();

            if (definitions is null)
            {
                await ReplyErrorAsync(Localization.SearchesDefinitionNotFound);
                return;
            }

            if (definitions.Count == 0)
            {
                await ReplyErrorAsync(Localization.SearchesDefinitionNotFound);
                return;
            }
            
            await SendPaginatedMessageAsync(definitions, 1, (items, _) =>
            {
                var definition = items.First();
                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Url = definition.Permalink,
                    Author = new LocalEmbedAuthorBuilder
                    {
                        Name = definition.Word,
                        IconUrl = "https://i.imgur.com/re5jokL.jpg"
                    },
                    Description = definition.Definition.Truncate(2000)
                };
                
                if (!string.IsNullOrEmpty(definition.Example))
                    embed.AddField(GetText(Localization.CommonExample), definition.Example.Truncate(1024));
                
                return embed;
            });
        }
        
        private class UrbanDictionaryDefinition
        {
            public string? Definition { get; set; }
            public string? Permalink { get; set; }
            public string? Word { get; set; }
            public string? Example { get; set; }
        }
    }
}