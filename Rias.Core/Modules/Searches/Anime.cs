using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Database.Models;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Core.Services.Commons;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Searches
{
    public partial class Searches
    {
        [Name("Anime")]
        public class Anime : RiasModule<AnimeService>
        {
            private readonly InteractiveService _interactive;
            
            public Anime(IServiceProvider services) : base(services)
            {
                _interactive = services.GetRequiredService<InteractiveService>();
            }

            [Command("anime"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task AnimeAsync([Remainder] string title)
            {
                var (type, method) = int.TryParse(title, out _) ? ("Int", "id") : ("String", "search");
                var query = AnimeService.AnimeQuery.Replace("[type]", type)
                    .Replace("[var]", method);
            
                var anime = await Service.GetAniListInfoAsync<AnimeMangaContent>(query, new {anime = title}, "Media");

                if (anime is null)
                {
                    await ReplyErrorAsync("AnimeNotFound");
                    return;
                }

                var episodeDuration = anime.Duration.HasValue
                    ? TimeSpan.FromMinutes(anime.Duration.Value).Humanize(2, Resources.GetGuildCulture(Context.Guild?.Id))
                    : "-";

                var startDate = "-";
                if (anime.StartDate.Year.HasValue && anime.StartDate.Month.HasValue && anime.StartDate.Day.HasValue)
                {
                    startDate = new DateTime(anime.StartDate.Year.Value, anime.StartDate.Month.Value, anime.StartDate.Day.Value)
                        .ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
                }
                
                var endDate = "-";
                if (anime.EndDate.Year.HasValue && anime.EndDate.Month.HasValue && anime.EndDate.Day.HasValue)
                {
                    endDate = new DateTime(anime.EndDate.Year.Value, anime.EndDate.Month.Value, anime.EndDate.Day.Value)
                        .ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
                }
                
                var genres = anime.Genres != null
                    ? anime.Genres.Length != 0
                        ? string.Join("\n", anime.Genres)
                        : "-"
                    : "-";

                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Url = anime.SiteUrl,
                        Title = $"{(string.IsNullOrEmpty(anime.Title.Romaji) ? anime.Title.English : anime.Title.Romaji)} (AniList)"
                    }.AddField(GetText("TitleRomaji"), !string.IsNullOrEmpty(anime.Title.Romaji) ? anime.Title.Romaji : "-", true)
                    .AddField(GetText("TitleEnglish"), !string.IsNullOrEmpty(anime.Title.English) ? anime.Title.English : "-", true)
                    .AddField(GetText("TitleNative"), !string.IsNullOrEmpty(anime.Title.Native) ? anime.Title.Native : "-", true)
                    .AddField(GetText("#Common_Id"), anime.Id, true)
                    .AddField(GetText("Format"), !string.IsNullOrEmpty(anime.Format) ? anime.Format : "-", true)
                    .AddField(GetText("Episodes"), anime.Episodes.HasValue ? anime.Episodes.Value.ToString() : "-", true)
                    .AddField(GetText("EpisodeDuration"), episodeDuration, true)
                    .AddField(GetText("#Utility_Status"), !string.IsNullOrEmpty(anime.Status) ? anime.Status : "-", true)
                    .AddField(GetText("StartDate"), startDate, true)
                    .AddField(GetText("EndDate"), endDate, true)
                    .AddField(GetText("Season"), !string.IsNullOrEmpty(anime.Season) ? anime.Season : "-", true)
                    .AddField(GetText("AverageScore"), anime.AverageScore.HasValue ? anime.AverageScore.Value.ToString() : "-", true)
                    .AddField(GetText("MeanScore"), anime.MeanScore.HasValue ? anime.MeanScore.Value.ToString() : "-", true)
                    .AddField(GetText("Popularity"), anime.Popularity.HasValue ? anime.Popularity.Value.ToString() : "-", true)
                    .AddField(GetText("Favourites"), anime.Favourites.HasValue ? anime.Favourites.Value.ToString() : "-", true)
                    .AddField(GetText("Source"), !string.IsNullOrEmpty(anime.Source) ? anime.Source : "-", true)
                    .AddField(GetText("Genres"), genres, true)
                    .AddField(GetText("IsAdult"), anime.IsAdult, true)
                    .AddField(GetText("Description"), !string.IsNullOrEmpty(anime.Description) ? anime.Description.Truncate(1000) : "-")
                    .WithImageUrl(anime.CoverImage.Large);

                await ReplyAsync(embed);
            }
            
            [Command("manga"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task MangaAsync([Remainder] string title)
            {
                var (type, method) = int.TryParse(title, out _) ? ("Int", "id") : ("String", "search");
                var query = AnimeService.MangaQuery.Replace("[type]", type)
                    .Replace("[var]", method);
            
                var manga = await Service.GetAniListInfoAsync<AnimeMangaContent>(query, new {manga = title}, "Media");

                if (manga is null)
                {
                    await ReplyErrorAsync("MangaNotFound");
                    return;
                }

                var startDate = "-";
                if (manga.StartDate.Year.HasValue && manga.StartDate.Month.HasValue && manga.StartDate.Day.HasValue)
                {
                    startDate = new DateTime(manga.StartDate.Year.Value, manga.StartDate.Month.Value, manga.StartDate.Day.Value)
                        .ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
                }
                
                var endDate = "-";
                if (manga.EndDate.Year.HasValue && manga.EndDate.Month.HasValue && manga.EndDate.Day.HasValue)
                {
                    endDate = new DateTime(manga.EndDate.Year.Value, manga.EndDate.Month.Value, manga.EndDate.Day.Value)
                        .ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
                }
                
                var synonyms = manga.Synonyms != null
                    ? manga.Synonyms.Length != 0
                        ? string.Join("\n", manga.Synonyms)
                        : "-"
                    : "-";
                
                var genres = manga.Genres != null
                    ? manga.Genres.Length != 0
                        ? string.Join("\n", manga.Genres)
                        : "-"
                    : "-";

                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Url = manga.SiteUrl,
                        Title = $"{(string.IsNullOrEmpty(manga.Title.Romaji) ? manga.Title.English : manga.Title.Romaji)} (AniList)"
                    }.AddField(GetText("TitleRomaji"), !string.IsNullOrEmpty(manga.Title.Romaji) ? manga.Title.Romaji : "-", true)
                    .AddField(GetText("TitleEnglish"), !string.IsNullOrEmpty(manga.Title.English) ? manga.Title.English : "-", true)
                    .AddField(GetText("TitleNative"), !string.IsNullOrEmpty(manga.Title.Native) ? manga.Title.Native : "-", true)
                    .AddField(GetText("#Common_Id"), manga.Id, true)
                    .AddField(GetText("Format"), !string.IsNullOrEmpty(manga.Format) ? manga.Format : "-", true)
                    .AddField(GetText("Chapters"), manga.Chapters.HasValue ? manga.Chapters.Value.ToString() : "-", true)
                    .AddField(GetText("Volumes"), manga.Volumes.HasValue ? manga.Volumes.Value.ToString() : "-", true)
                    .AddField(GetText("#Utility_Status"), !string.IsNullOrEmpty(manga.Status) ? manga.Status : "-", true)
                    .AddField(GetText("StartDate"), startDate, true)
                    .AddField(GetText("EndDate"), endDate, true)
                    .AddField(GetText("AverageScore"), manga.AverageScore.HasValue ? manga.AverageScore.Value.ToString() : "-", true)
                    .AddField(GetText("MeanScore"), manga.MeanScore.HasValue ? manga.MeanScore.Value.ToString() : "-", true)
                    .AddField(GetText("Popularity"), manga.Popularity.HasValue ? manga.Popularity.Value.ToString() : "-", true)
                    .AddField(GetText("Favourites"), manga.Favourites.HasValue ? manga.Favourites.Value.ToString() : "-", true)
                    .AddField(GetText("Source"), !string.IsNullOrEmpty(manga.Source) ? manga.Source : "-", true)
                    .AddField(GetText("Genres"), genres, true)
                    .AddField(GetText("Synonyms"), synonyms, true)
                    .AddField(GetText("IsAdult"), manga.IsAdult, true)
                    .AddField(GetText("Description"), !string.IsNullOrEmpty(manga.Description) ? manga.Description.Truncate(1000) : "-")
                    .WithImageUrl(manga.CoverImage.Large);

                await ReplyAsync(embed);
            }
            
            [Command("character"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task CharacterAsync([Remainder] string name)
            {
                CustomCharacters? character;
                if (name.StartsWith("@") && int.TryParse(name[1..], out var id))
                {
                    character = await DbContext.CustomCharacters.FirstOrDefaultAsync(x => x.CharacterId == id);
                }
                else if (int.TryParse(name, out id))
                {
                    await AniListCharacterAsync(name);
                    return;
                }
                else
                {
                    character = DbContext.CustomCharacters
                        .AsEnumerable()
                        .FirstOrDefault(x =>
                            name.Split(' ')
                                .All(y => x.Name!.Contains(y, StringComparison.InvariantCultureIgnoreCase)));
                    
                    if (character is null)
                    {
                        await AniListCharacterAsync(name);
                        return;
                    }
                }

                if (character is null)
                {
                    await ReplyErrorAsync("CharacterNotFound");
                    return;
                }
                
                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Title = character.Name
                    }.AddField(GetText("#Common_Id"), character.Id, true)
                    .AddField(GetText("Source"), GetText("#Bot_Database"), true)
                    .AddField(GetText("Description"), !string.IsNullOrEmpty(character.Description) ? character.Description.Truncate(1000) : "-")
                    .WithImageUrl(character.ImageUrl);

                await ReplyAsync(embed);
            }
            
            private async Task AniListCharacterAsync(string name)
            {
                var (type, method) = int.TryParse(name, out _) ? ("Int", "id") : ("String", "search");
                var query = AnimeService.CharacterQuery.Replace("[type]", type)
                    .Replace("[var]", method);
            
                var character = await Service.GetAniListInfoAsync<CharacterContent>(query, new {character = name}, "Character");

                if (character is null)
                {
                    await ReplyErrorAsync("CharacterNotFound");
                    return;
                }
                
                var alternativeNames = character.Name.Alternative!.Where(x => !string.IsNullOrEmpty(x)).ToList();
                var alternative = alternativeNames.Count != 0
                    ? string.Join("\n", alternativeNames)
                    : "-";

                var mangaList = character.Media.Nodes!
                    .Where(x => string.Equals(x.Type, "manga", StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"[{(string.IsNullOrEmpty(x.Title.Romaji) ? x.Title.English : x.Title.Romaji)}]({x.SiteUrl}) | {x.Id}")
                    .ToList();
                var manga = mangaList.Count != 0
                    ? string.Join("\n", mangaList)
                    : "-";
                
                var animeList = character.Media.Nodes!
                    .Where(x => string.Equals(x.Type, "anime", StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"[{(string.IsNullOrEmpty(x.Title.Romaji) ? x.Title.English : x.Title.Romaji)}]({x.SiteUrl}) | {x.Id}")
                    .ToList();
                var anime = animeList.Count != 0
                    ? string.Join("\n", animeList)
                    : "-";

                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Url = character.SiteUrl,
                    Title = $"{character.Name.First} {character.Name.Last}"
                }.AddField(GetText("FirstName"), !string.IsNullOrEmpty(character.Name.First) ? character.Name.First : "-", true)
                    .AddField(GetText("LastName"), !string.IsNullOrEmpty(character.Name.Last) ? character.Name.Last : "-", true)
                    .AddField(GetText("NativeName"), !string.IsNullOrEmpty(character.Name.Native) ? character.Name.Native : "-", true)
                    .AddField(GetText("Alternative"), alternative, true)
                    .AddField(GetText("#Common_Id"), character.Id, true)
                    .AddField(GetText("Favourites"), character.Favourites.HasValue ? character.Favourites.Value.ToString() : "-", true)
                    .AddField(GetText("FromManga"), manga, true)
                    .AddField(GetText("FromAnime"), anime, true)
                    .AddField(GetText("Source"), "AniList", true)
                    .AddField(GetText("Description"), !string.IsNullOrEmpty(character.Description) ? character.Description.Truncate(1000) : "-")
                    .WithImageUrl(character.Image.Large);

                await ReplyAsync(embed);
            }

            [Command("animelist"),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task AnimeListAsync([Remainder] string title)
            {
                var animeList = await Service.GetAniListInfoAsync<List<AnimeMangaContent>>(AnimeService.AnimeListQuery, new {anime = title}, "Page", "media");
                if (animeList is null || animeList.Count == 0)
                {
                    await ReplyErrorAsync("AnimeListNotFound");
                    return;
                }

                var pages = animeList.Batch(10).Select(x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = GetText("AnimeList", title, Context.Prefix),
                        Color = RiasUtils.ConfirmColor,
                        Description = string.Join("\n", x.Select(c =>
                            $"[{(string.IsNullOrEmpty(c.Title.Romaji) ? c.Title.English : c.Title.Romaji)}]({c.SiteUrl}) | {c.Id}"))
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }
            
            [Command("mangalist"),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task MangaListAsync([Remainder] string title)
            {
                var mangaList = await Service.GetAniListInfoAsync<List<AnimeMangaContent>>(AnimeService.MangaListQuery, new {manga = title}, "Page", "media");
                if (mangaList is null || mangaList.Count == 0)
                {
                    await ReplyErrorAsync("MangaListNotFound");
                    return;
                }
                
                var pages = mangaList.Batch(10).Select(x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = GetText("MangaList", title, Context.Prefix),
                        Color = RiasUtils.ConfirmColor,
                        Description = string.Join("\n", x.Select(c =>
                            $"[{(string.IsNullOrEmpty(c.Title.Romaji) ? c.Title.English : c.Title.Romaji)}]({c.SiteUrl}) | {c.Id}"))
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }
            
            [Command("characters"),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task CharactersAsync([Remainder] string name)
            {
                var characters = await Service.GetAniListInfoAsync<List<CharacterContent>>(AnimeService.CharacterListQuery, new {character = name}, "Page", "characters");
                if (characters is null || characters.Count == 0)
                {
                    await ReplyErrorAsync("CharactersNotFound");
                    return;
                }
                
                var pages = characters.Batch(10).Select(x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = GetText("CharacterList", name, Context.Prefix),
                        Color = RiasUtils.ConfirmColor,
                        Description = string.Join("\n", x.Select(c =>
                            $"[{c.Name.First} {c.Name.Last}]({c.SiteUrl}) | {c.Id}"))
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }
        }
    }
}