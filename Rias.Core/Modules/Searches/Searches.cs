using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Qmmands;
using Rias.Core.Implementation;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Searches
{
    [Name("Searches")]
    public partial class Searches : RiasModule
    {
        private readonly HttpClient _httpClient;
        private readonly InteractiveService _interactive;
        
        public Searches(IServiceProvider services) : base(services)
        {
            _httpClient = services.GetRequiredService<HttpClient>();
            _interactive = services.GetRequiredService<InteractiveService>();
        }

        [Command("wikipedia")]
        public async Task WikipediaAsync([Remainder] string title)
        {
            await Context.Channel.TriggerTypingAsync();
            using var response = await _httpClient.GetAsync("https://en.wikipedia.org//w/api.php?action=query&format=json&prop=info&redirects=1&formatversion=2&inprop=url&titles=" +
                                                            Uri.EscapeDataString(title));
            if (!response.IsSuccessStatusCode)
            {
                await ReplyErrorAsync("NotFound");
                return;
            }

            var result = await response.Content.ReadAsStringAsync();
            var page = JObject.Parse(result)
                .SelectToken("query.pages")?[0];

            if (page is null)
            {
                await ReplyErrorAsync("NotFound");
                return;
            }

            if (page.Value<bool>("missing"))
                await ReplyErrorAsync("NotFound");
            else
                await Context.Channel.SendMessageAsync(page.Value<string>("fullurl"));
        }

        [Command("urbandictionary")]
        public async Task UrbanDictionary([Remainder] string term)
        {
            if (string.IsNullOrEmpty(Credentials.UrbanDictionaryApiKey))
            {
                await ReplyErrorAsync("UrbanDictionaryNoApiKey");
                return;
            }
            
            await Context.Channel.TriggerTypingAsync();
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-rapidapi-key", Credentials.UrbanDictionaryApiKey);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            using var response = await httpClient.GetAsync($"https://mashape-community-urban-dictionary.p.rapidapi.com/define?term={Uri.EscapeUriString(term)}");
            if (!response.IsSuccessStatusCode)
            {
                await ReplyErrorAsync("DefinitionNotFound");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var definitions = JObject.Parse(content)
                .SelectToken("list")?
                .ToObject<List<UrbanDictionaryDefinition>>();

            if (definitions is null)
            {
                await ReplyErrorAsync("DefinitionNotFound");
                return;
            }

            if (definitions.Count == 0)
            {
                await ReplyErrorAsync("DefinitionNotFound");
                return;
            }

            var pages = definitions.Select(x =>
            {
                var interactiveMessage = new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Url = x.Permalink,
                        Author = new EmbedAuthorBuilder
                        {
                            Name = x.Word,
                            IconUrl = "https://i.imgur.com/re5jokL.jpg"
                        },
                        Description = x.Definition.Truncate(2000)
                    }
                );
                if (!string.IsNullOrEmpty(x.Example))
                    interactiveMessage.EmbedBuilder!.AddField(GetText("#Common_Example"), x.Example.Truncate(1024));
                return interactiveMessage;
            });

            await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
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