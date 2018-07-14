using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Searches.Services;
using RiasBot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Discord.Addons.Interactive;

namespace RiasBot.Modules.Searches
{
    public partial class Searches
    {
        public class AnimeCommands : RiasSubmodule<AnimeService>
        {
            public readonly CommandHandler _ch;
            private InteractiveService _is;

            public AnimeCommands(CommandHandler ch, InteractiveService interactiveService)
            {
                _ch = ch;
                _is = interactiveService;
            }

            [RiasCommand] [@Alias] [Description] [@Remarks]
            public async Task Anime([Remainder]string anime)
            {
                var obj = await _service.AnimeSearch(anime);

                if (obj is null)
                    await Context.Channel.SendErrorEmbed("I couldn't find the anime.");
                else
                {
                    var title = $"{(string)obj.title.romaji ?? (string)obj.title.english} (AniList URL)";
                    var titleRomaji = (string)obj.title.romaji;
                    var titleEnglish = (string)obj.title.english;
                    var titleNative = (string)obj.title.native;

                    if (String.IsNullOrEmpty(titleRomaji))
                        titleRomaji = "-";
                    if (String.IsNullOrEmpty(titleEnglish))
                        titleEnglish = "-";
                    if (String.IsNullOrEmpty(titleNative))
                        titleNative = "-";

                    var startDate = $"{(string)obj.startDate.day}.{(string)obj.startDate.month}.{(string)obj.startDate.year}";
                    var endDate = $"{(string)obj.endDate.day}.{(string)obj.endDate.month}.{(string)obj.endDate.year}";
                    if (startDate == "..")
                        startDate = "-";
                    if (endDate == "..")
                        endDate = "-";
                    var episodes = "-";
                    var averageScore = "-";
                    var meanScore = "-";
                    var duration = "-";
                    var genres = String.Join("\n", (JArray)obj.genres);
                    if (String.IsNullOrEmpty(genres))
                        genres = "-";
                    try
                    {
                        episodes = $"{(int)obj.episodes}";
                        averageScore = $"{(int)obj.averageScore} %";
                        meanScore = $"{(int)obj.meanScore} %";
                        duration = $"{(int)obj.duration} mins";
                    }
                    catch
                    {
                        
                    }
                    var description = (string)obj.description;
                    description = description.Replace("<br>", "");
                    if (description.Length > 1024)
                        description = $"{description.Substring(0, 950)}... [More]({(string)obj.siteUrl})";

                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);

                    embed.WithAuthor(title, null, (string)obj.siteUrl);
                    embed.AddField("Romaji", titleRomaji, true).AddField("English", titleEnglish, true).AddField("Native", titleNative, true);
                    embed.AddField("ID", (int)obj.id, true).AddField("Type", (string)obj.format, true).AddField("Episodes", episodes, true);
                    embed.AddField("Status", (string)obj.status, true).AddField("Start", startDate, true).AddField("End", endDate, true);
                    embed.AddField("Average Score", averageScore, true).AddField("Mean Score", meanScore, true).AddField("Popularity", (int)obj.popularity, true);
                    embed.AddField("Duration", duration, true).AddField("Genres", genres, true).AddField("Is Adult", (bool)obj.isAdult, true);
                    embed.AddField("Description", description);
                    embed.WithImageUrl((string)obj.coverImage.large);
                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(1)]
            public async Task Character([Remainder]int character)
            {
                var obj = await _service.CharacterSearch(character);

                if (obj is null)
                    await Context.Channel.SendErrorEmbed("I couldn't find the character.");
                else
                {
                    var name = $"{(string)obj.name.first} {(string)obj.name.last} (AniList URL)";
                    var firstName = (string)obj.name.first;
                    var lastName = (string)obj.name.last;
                    var nativeName = (string)obj.name.native;

                    if (String.IsNullOrEmpty(firstName))
                        firstName = "-";
                    if (String.IsNullOrEmpty(lastName))
                        lastName = "-";
                    if (String.IsNullOrEmpty(nativeName))
                        nativeName = "-";

                    var alternative = String.Join(",\n", (JArray)obj.name.alternative);
                    var description = (string)obj.description;
                    if (!String.IsNullOrEmpty(description))
                    {
                        if (description.Length > 1024)
                            description = $"{description.Substring(0, 950)}... [More]({(string)obj.siteUrl})";
                    }
                    else
                    {
                        description = "-";
                    }

                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);

                    embed.WithAuthor(name, null, (string)obj.siteUrl);
                    embed.AddField("First Name", firstName, true).AddField("Last Name", lastName, true).AddField("Native", nativeName, true);
                    embed.AddField("Alternative", (alternative != "") ? alternative : "-", true).AddField("Id", (int)obj.id);
                    embed.AddField("Description", (description != "") ? description : "-");
                    embed.WithImageUrl((string)obj.image.large);
                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(0)]
            public async Task Character([Remainder]string character)
            {
                var obj = await _service.CharacterSearch(character);

                var characters = (JArray)obj.characters;
                if (characters.Count == 0)
                    await Context.Channel.SendErrorEmbed("I couldn't find the character.");
                else
                {
                    if (characters.Count <= 1)
                    {
                        var name = $"{(string)obj.characters[0].name.first} {(string)obj.characters[0].name.last} (AniList URL)";
                        var firstName = (string)obj.characters[0].name.first;
                        var lastName = (string)obj.characters[0].name.last;
                        var nativeName = (string)obj.characters[0].name.native;

                        if (String.IsNullOrEmpty(firstName))
                            firstName = "-";
                        if (String.IsNullOrEmpty(lastName))
                            lastName = "-";
                        if (String.IsNullOrEmpty(nativeName))
                            nativeName = "-";
                        var alternative = String.Join(", ", (JArray)obj.characters[0].name.alternative);
                        var description = (string)obj.characters[0].description;
                        if (!String.IsNullOrEmpty(description))
                        {
                            if (description.Length > 1024)
                                description = $"{description.Substring(0, 950)}... [More]({(string)obj.characters[0].siteUrl})";
                        }
                        else
                        {
                            description = "-";
                        }

                        var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);

                        embed.WithAuthor(name, null, (string)obj.characters[0].siteUrl);
                        embed.AddField("First Name", firstName, true).AddField("Last Name", lastName, true).AddField("Native", nativeName, true);
                        embed.AddField("Alternative", (alternative != "") ? alternative : "-", true).AddField("Id", (int)obj.characters[0].id);
                        embed.AddField("Description", description);
                        embed.WithImageUrl((string)obj.characters[0].image.large);
                        await Context.Channel.SendMessageAsync("", embed: embed.Build());
                    }
                    else
                    {
                        var listCharacters = new List<string>();
                        for (var i = 0; i < characters.Count(); i++)
                        {
                            var waifuName1 = $"{(string)obj.characters[i].name.first} { (string)obj.characters[i].name.last}";
                            listCharacters.Add($"{waifuName1}\tId: {obj.characters[i].id}\n");
                        }
                        var pager = new PaginatedMessage
                        {
                            Title = $"I've found {characters.Count()} characters for {character}. Search the character by id",
                            Color = new Color(RiasBot.GoodColor),
                            Pages = listCharacters,
                            Options = new PaginatedAppearanceOptions
                            {
                                ItemsPerPage = 10,
                                Timeout = TimeSpan.FromMinutes(1),
                                DisplayInformationIcon = false,
                                JumpDisplayOptions = JumpDisplayOptions.Never
                            }

                        };
                        await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task AnimeList([Remainder]string anime)
            {
                var obj = await _service.AnimeListSearch(anime);

                if (obj is null)
                    await Context.Channel.SendErrorEmbed("I couldn't find anime.");
                else
                {
                    string description = null;
                    for (var i = 0; i < 10; i++)
                    {
                        try
                        {
                            description += $"#{i+1} [{obj.media[i].title.romaji}]({obj.media[i].siteUrl})\n";
                        }
                        catch
                        {
                            break;
                        }
                    }
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithDescription(description);

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }
        }
    }
}
