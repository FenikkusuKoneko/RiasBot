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

namespace RiasBot.Modules.Searches
{
    public partial class Searches
    {
        public class AnimeCommands : RiasSubmodule<AnimeService>
        {
            public readonly CommandHandler _ch;

            public AnimeCommands(CommandHandler ch, CommandService service)
            {
                _ch = ch;
            }

            [RiasCommand] [@Alias] [Description] [@Remarks]
            public async Task Anime([Remainder]string anime)
            {
                var obj = await _service.AnimeSearch(anime);

                if (obj is null)
                    await ReplyAsync("I couldn't find the anime.");
                else
                {
                    string title = $"{(string)obj.title.romaji ?? (string)obj.title.english} (AniList URL)";
                    string titleRomaji = (string)obj.title.romaji;
                    string titleEnglish = (string)obj.title.english;
                    string titleNative = (string)obj.title.native;

                    if (String.IsNullOrEmpty(titleRomaji))
                        titleRomaji = "-";
                    if (String.IsNullOrEmpty(titleEnglish))
                        titleEnglish = "-";
                    if (String.IsNullOrEmpty(titleNative))
                        titleNative = "-";

                    string startDate = $"{(string)obj.startDate.day}.{(string)obj.startDate.month}.{(string)obj.startDate.year}";
                    string endDate = $"{(string)obj.endDate.day}.{(string)obj.endDate.month}.{(string)obj.endDate.year}";
                    if (startDate == "..")
                        startDate = "-";
                    if (endDate == "..")
                        endDate = "-";
                    string episodes = "-";
                    string averageScore = "-";
                    string meanScore = "-";
                    string duration = "-";
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
                    string description = (string)obj.description;
                    if (description.Length > 1024)
                        description = description.Substring(0, 1024);

                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);

                    embed.WithAuthor(title, null, (string)obj.siteUrl);
                    embed.AddField("Romaji", titleRomaji, true).AddField("English", titleEnglish, true).AddField("Native", titleNative, true);
                    embed.AddField("ID", (int)obj.id, true).AddField("Type", (string)obj.format, true).AddField("Episodes", episodes, true);
                    embed.AddField("Status", (string)obj.status, true).AddField("Start", startDate, true).AddField("End", endDate, true);
                    embed.AddField("Average Score", averageScore, true).AddField("Mean Score", meanScore, true).AddField("Popularity", (int)obj.popularity, true);
                    embed.AddField("Duration", duration, true).AddField("Genres", String.Join("\n", (JArray)obj.genres), true).AddField("Is Adult", (bool)obj.isAdult, true);
                    embed.AddField("Description", description);
                    embed.WithImageUrl((string)obj.coverImage.large);
                    await ReplyAsync("", embed: embed.Build());
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(1)]
            public async Task Character([Remainder]int character)
            {
                var obj = await _service.CharacterSearch(character);

                if (obj is null)
                    await ReplyAsync("Sorry I couldn't find the character.");
                else
                {
                    string name = $"{(string)obj.name.first} {(string)obj.name.last} (AniList URL)";
                    string alternative = String.Join(",\n", (JArray)obj.name.alternative);
                    string description = (string)obj.description;
                    if (description.Length > 1024)
                        description = description.Substring(0, 1024);

                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);

                    embed.WithAuthor(name, null, (string)obj.siteUrl);
                    embed.AddField("First Name", (string)obj.name.first ?? "-", true).AddField("Last Name", (string)obj.name.last ?? "-", true)
                        .AddField("Native", (string)obj.name.native ?? "-", true);
                    embed.AddField("Alternative", (alternative != "") ? alternative : "-", true).AddField("Id", (int)obj.id);
                    embed.AddField("Description", (description != "") ? description : "-");
                    embed.WithImageUrl((string)obj.image.large);
                    await ReplyAsync("", embed: embed.Build());
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
                    await ReplyAsync("Sorry I couldn't find the character.");
                else
                {
                    if (characters.Count <= 1)
                    {
                        string name = $"{(string)obj.characters[0].name.first} {(string)obj.characters[0].name.last} (AniList URL)";
                        string alternative = String.Join(", ", (JArray)obj.characters[0].name.alternative);
                        string description = (string)obj.characters[0].description;
                        if (description.Length > 1024)
                            description = description.Substring(0, 1024);

                        var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);

                        embed.WithAuthor(name, null, (string)obj.characters[0].siteUrl);
                        embed.AddField("First Name", (string)obj.characters[0].name.first ?? "-", true).AddField("Last Name", (string)obj.characters[0].name.last ?? "-", true)
                            .AddField("Native", (string)obj.characters[0].name.native ?? "-", true);
                        embed.AddField("Alternative", (alternative != "") ? alternative : "-", true).AddField("Id", (int)obj.characters[0].id);
                        embed.AddField("Description", description);
                        embed.WithImageUrl((string)obj.characters[0].image.large);
                        await ReplyAsync("", embed: embed.Build());
                    }
                    else
                    {
                        string[] listCharacters = new string[characters.Count];
                        for (int i = 0; i < characters.Count(); i++)
                        {
                            string waifuName1 = $"{(string)obj.characters[i].name.first} { (string)obj.characters[i].name.last}";
                            listCharacters[i] = $"{waifuName1}\tId: {obj.characters[i].id}\n";
                        }
                        await Context.Channel.SendPaginated((DiscordSocketClient)Context.Client, $"I've found {characters.Count()} characters for {character}. Search the character by id",
                            listCharacters, 10);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task AnimeList([Remainder]string anime)
            {
                var obj = await _service.AnimeListSearch(anime);

                if (obj is null)
                    await ReplyAsync("Sorry I couldn't find anime.");
                else
                {
                    string description = null;
                    for (int i = 0; i < 10; i++)
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
                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                    embed.WithDescription(description);

                    await ReplyAsync("", embed: embed.Build());
                }
            }
        }
    }
}
