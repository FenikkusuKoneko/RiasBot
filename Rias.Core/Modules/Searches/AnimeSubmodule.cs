using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Core.Services.Commons;

namespace Rias.Core.Modules.Searches
{
    public partial class SearchesModule
    {
        [Name("Anime")]
        public class AnimeSubmodule : RiasModule<AnimeService>
        {
            public AnimeSubmodule(IServiceProvider services) : base(services)
            {
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
                    await ReplyErrorAsync(Localization.SearchesAnimeNotFound);
                    return;
                }

                var episodeDuration = anime.Duration.HasValue
                    ? TimeSpan.FromMinutes(anime.Duration.Value).Humanize(2, new CultureInfo(Localization.GetGuildLocale(Context.Guild?.Id)))
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

                var embed = new LocalEmbedBuilder()
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Url = anime.SiteUrl,
                        Title = $"{(string.IsNullOrEmpty(anime.Title.Romaji) ? anime.Title.English : anime.Title.Romaji)} (AniList)"
                    }.AddField(GetText(Localization.SearchesTitleRomaji), !string.IsNullOrEmpty(anime.Title.Romaji) ? anime.Title.Romaji : "-", true)
                    .AddField(GetText(Localization.SearchesTitleEnglish), !string.IsNullOrEmpty(anime.Title.English) ? anime.Title.English : "-", true)
                    .AddField(GetText(Localization.SearchesTitleNative), !string.IsNullOrEmpty(anime.Title.Native) ? anime.Title.Native : "-", true)
                    .AddField(GetText(Localization.CommonId), anime.Id, true)
                    .AddField(GetText(Localization.SearchesFormat), !string.IsNullOrEmpty(anime.Format) ? anime.Format : "-", true)
                    .AddField(GetText(Localization.SearchesEpisodes), anime.Episodes.HasValue ? anime.Episodes.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesEpisodeDuration), episodeDuration, true)
                    .AddField(GetText(Localization.UtilityStatus), !string.IsNullOrEmpty(anime.Status) ? anime.Status : "-", true)
                    .AddField(GetText(Localization.SearchesStartDate), startDate, true)
                    .AddField(GetText(Localization.SearchesEndDate), endDate, true)
                    .AddField(GetText(Localization.SearchesSeason), !string.IsNullOrEmpty(anime.Season) ? anime.Season : "-", true)
                    .AddField(GetText(Localization.SearchesAverageScore), anime.AverageScore.HasValue ? anime.AverageScore.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesMeanScore), anime.MeanScore.HasValue ? anime.MeanScore.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesPopularity), anime.Popularity.HasValue ? anime.Popularity.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesFavourites), anime.Favourites.HasValue ? anime.Favourites.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesSource), !string.IsNullOrEmpty(anime.Source) ? anime.Source : "-", true)
                    .AddField(GetText(Localization.SearchesGenres), genres, true)
                    .AddField(GetText(Localization.SearchesIsAdult), anime.IsAdult, true)
                    .AddField(GetText(Localization.SearchesDescription), !string.IsNullOrEmpty(anime.Description) ? anime.Description.Truncate(1000) : "-")
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
                    await ReplyErrorAsync(Localization.SearchesMangaNotFound);
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

                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Url = manga.SiteUrl,
                        Title = $"{(string.IsNullOrEmpty(manga.Title.Romaji) ? manga.Title.English : manga.Title.Romaji)} (AniList)"
                    }.AddField(GetText(Localization.SearchesTitleRomaji), !string.IsNullOrEmpty(manga.Title.Romaji) ? manga.Title.Romaji : "-", true)
                    .AddField(GetText(Localization.SearchesTitleEnglish), !string.IsNullOrEmpty(manga.Title.English) ? manga.Title.English : "-", true)
                    .AddField(GetText(Localization.SearchesTitleNative), !string.IsNullOrEmpty(manga.Title.Native) ? manga.Title.Native : "-", true)
                    .AddField(GetText(Localization.CommonId), manga.Id, true)
                    .AddField(GetText(Localization.SearchesFormat), !string.IsNullOrEmpty(manga.Format) ? manga.Format : "-", true)
                    .AddField(GetText(Localization.SearchesChapters), manga.Chapters.HasValue ? manga.Chapters.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesVolumes), manga.Volumes.HasValue ? manga.Volumes.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.UtilityStatus), !string.IsNullOrEmpty(manga.Status) ? manga.Status : "-", true)
                    .AddField(GetText(Localization.SearchesStartDate), startDate, true)
                    .AddField(GetText(Localization.SearchesEndDate), endDate, true)
                    .AddField(GetText(Localization.SearchesAverageScore), manga.AverageScore.HasValue ? manga.AverageScore.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesMeanScore), manga.MeanScore.HasValue ? manga.MeanScore.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesPopularity), manga.Popularity.HasValue ? manga.Popularity.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesFavourites), manga.Favourites.HasValue ? manga.Favourites.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesSource), !string.IsNullOrEmpty(manga.Source) ? manga.Source : "-", true)
                    .AddField(GetText(Localization.SearchesGenres), genres, true)
                    .AddField(GetText(Localization.SearchesSynonyms), synonyms, true)
                    .AddField(GetText(Localization.SearchesIsAdult), manga.IsAdult, true)
                    .AddField(GetText(Localization.SearchesDescription), !string.IsNullOrEmpty(manga.Description) ? manga.Description.Truncate(1000) : "-")
                    .WithImageUrl(manga.CoverImage.Large);

                await ReplyAsync(embed);
            }
            
            [Command("character"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task CharacterAsync([Remainder] string name)
            {
                CustomCharactersEntity? character;
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
                    await ReplyErrorAsync(Localization.SearchesCharacterNotFound);
                    return;
                }
                
                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = character.Name
                    }.AddField(GetText(Localization.CommonId), character.Id, true)
                    .AddField(GetText(Localization.SearchesSource), GetText(Localization.BotDatabase), true)
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
                    await ReplyErrorAsync(Localization.SearchesCharacterNotFound);
                    return;
                }
                
                var alternativeNames = character.Name.Alternative!.Where(x => !string.IsNullOrEmpty(x)).ToList();
                var alternative = alternativeNames.Count != 0
                    ? string.Join("\n", alternativeNames)
                    : "-";

                var mangaList = GetCharacterSources(character, "manga");
                var manga = mangaList.Count != 0
                    ? string.Join("\n", mangaList)
                    : "-";
                
                var animeList = GetCharacterSources(character, "anime");
                var anime = animeList.Count != 0
                    ? string.Join("\n", animeList)
                    : "-";

                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Url = character.SiteUrl,
                    Title = $"{character.Name.First} {character.Name.Last}"
                }.AddField(GetText(Localization.SearchesFirstName), !string.IsNullOrEmpty(character.Name.First) ? character.Name.First : "-", true)
                    .AddField(GetText(Localization.SearchesLastName), !string.IsNullOrEmpty(character.Name.Last) ? character.Name.Last : "-", true)
                    .AddField(GetText(Localization.SearchesNativeName), !string.IsNullOrEmpty(character.Name.Native) ? character.Name.Native : "-", true)
                    .AddField(GetText(Localization.SearchesAlternative), alternative, true)
                    .AddField(GetText(Localization.CommonId), character.Id, true)
                    .AddField(GetText(Localization.SearchesFavourites), character.Favourites.HasValue ? character.Favourites.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesFromManga), manga, true)
                    .AddField(GetText(Localization.SearchesFromAnime), anime, true)
                    .AddField(GetText(Localization.SearchesSource), "AniList", true)
                    .AddField(GetText(Localization.SearchesDescription), !string.IsNullOrEmpty(character.Description) ? character.Description.Truncate(1000) : "-")
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
                    await ReplyErrorAsync(Localization.SearchesAnimeListNotFound);
                    return;
                }

                await SendPaginatedMessageAsync(animeList, 10, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.SearchesAnimeList, title, Context.Prefix),
                    Description = string.Join("\n", items.Select(c =>
                        $"[{(string.IsNullOrEmpty(c.Title.Romaji) ? c.Title.English : c.Title.Romaji)}]({c.SiteUrl}) ({c.Id})"))
                });
            }
            
            [Command("mangalist"),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task MangaListAsync([Remainder] string title)
            {
                var mangaList = await Service.GetAniListInfoAsync<List<AnimeMangaContent>>(AnimeService.MangaListQuery, new {manga = title}, "Page", "media");
                if (mangaList is null || mangaList.Count == 0)
                {
                    await ReplyErrorAsync(Localization.SearchesMangaListNotFound);
                    return;
                }
                
                await SendPaginatedMessageAsync(mangaList, 10, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.SearchesMangaList, title, Context.Prefix),
                    Description = string.Join("\n", items.Select(c =>
                        $"[{(string.IsNullOrEmpty(c.Title.Romaji) ? c.Title.English : c.Title.Romaji)}]({c.SiteUrl}) ({c.Id})"))
                });
            }
            
            [Command("characters"),
             Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task CharactersAsync([Remainder] string name)
            {
                var characters = await Service.GetAniListInfoAsync<List<CharacterContent>>(AnimeService.CharacterListQuery, new {character = name}, "Page", "characters");
                if (characters is null || characters.Count == 0)
                {
                    await ReplyErrorAsync(Localization.SearchesCharactersNotFound);
                    return;
                }

                await SendPaginatedMessageAsync(characters, 10, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.SearchesCharacterList, name, Context.Prefix),
                    Description = string.Join("\n", items.Select(c =>
                    {
                        var fromAnime = GetCharacterSources(c, "anime").FirstOrDefault();
                        var from = fromAnime != null
                            ? $"{GetText(Localization.SearchesFromAnime)}: {fromAnime}"
                            : $"{GetText(Localization.SearchesFromAnime)}: {GetCharacterSources(c, "manga").FirstOrDefault()}";
                        
                        return $"• [{c.Name.First} {c.Name.Last}]({c.SiteUrl}) ({c.Id}) | {from}";
                    }))
                });
            }

            private IList<string> GetCharacterSources(CharacterContent character, string from)
                => character.Media.Nodes!
                    .Where(x => string.Equals(x.Type, from, StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"[{(string.IsNullOrEmpty(x.Title.Romaji) ? x.Title.English : x.Title.Romaji)}]({x.SiteUrl}) ({x.Id})")
                    .ToList();
        }
    }
}