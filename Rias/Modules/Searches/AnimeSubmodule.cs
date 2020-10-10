using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Implementation;
using Rias.Services;
using Rias.Services.Commons;

namespace Rias.Modules.Searches
{
    public partial class SearchesModule
    {
        [Name("Anime")]
        public class AnimeSubmodule : RiasModule<AnimeService>
        {
            public AnimeSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            [Command("anime")]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task AnimeAsync([Remainder] string title)
            {
                var (type, method) = int.TryParse(title, out _) ? ("Int", "id") : ("String", "search");
                var query = AnimeService.AnimeQuery.Replace("[type]", type)
                    .Replace("[var]", method);
            
                var anime = await Service.GetAniListInfoAsync<AnimeMangaContent>(query, new { anime = title }, "Media");
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

                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Url = anime.SiteUrl,
                        Title = $"{(string.IsNullOrEmpty(anime.Title.Romaji) ? anime.Title.English : anime.Title.Romaji)} (AniList)"
                    }.AddField(GetText(Localization.SearchesTitleRomaji), !string.IsNullOrEmpty(anime.Title.Romaji) ? anime.Title.Romaji : "-", true)
                    .AddField(GetText(Localization.SearchesTitleEnglish), !string.IsNullOrEmpty(anime.Title.English) ? anime.Title.English : "-", true)
                    .AddField(GetText(Localization.SearchesTitleNative), !string.IsNullOrEmpty(anime.Title.Native) ? anime.Title.Native : "-", true)
                    .AddField(GetText(Localization.CommonId), anime.Id.ToString(), true)
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
                    .AddField(GetText(Localization.SearchesIsAdult), anime.IsAdult.ToString(), true)
                    .AddField(GetText(Localization.SearchesDescription), !string.IsNullOrEmpty(anime.Description)
                        ? $"{anime.Description.Truncate(900)} [{GetText(Localization.More).ToLowerInvariant()}]({anime.SiteUrl})"
                        : "-")
                    .WithImageUrl(anime.CoverImage.Large);

                await ReplyAsync(embed);
            }

            [Command("manga")]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task MangaAsync([Remainder] string title)
            {
                var (type, method) = int.TryParse(title, out _) ? ("Int", "id") : ("String", "search");
                var query = AnimeService.MangaQuery.Replace("[type]", type)
                    .Replace("[var]", method);
            
                var manga = await Service.GetAniListInfoAsync<AnimeMangaContent>(query, new { manga = title }, "Media");
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

                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Url = manga.SiteUrl,
                        Title = $"{(string.IsNullOrEmpty(manga.Title.Romaji) ? manga.Title.English : manga.Title.Romaji)} (AniList)"
                    }.AddField(GetText(Localization.SearchesTitleRomaji), !string.IsNullOrEmpty(manga.Title.Romaji) ? manga.Title.Romaji : "-", true)
                    .AddField(GetText(Localization.SearchesTitleEnglish), !string.IsNullOrEmpty(manga.Title.English) ? manga.Title.English : "-", true)
                    .AddField(GetText(Localization.SearchesTitleNative), !string.IsNullOrEmpty(manga.Title.Native) ? manga.Title.Native : "-", true)
                    .AddField(GetText(Localization.CommonId), manga.Id.ToString(), true)
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
                    .AddField(GetText(Localization.SearchesIsAdult), manga.IsAdult.ToString(), true)
                    .AddField(GetText(Localization.SearchesDescription), !string.IsNullOrEmpty(manga.Description)
                        ? $"{manga.Description.Truncate(900)} [{GetText(Localization.More.ToLowerInvariant())}]({manga.SiteUrl})"
                        : "-")
                    .WithImageUrl(manga.CoverImage.Large);

                await ReplyAsync(embed);
            }

            [Command("character")]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task CharacterAsync([Remainder] string name)
            {
                CustomCharactersEntity? character;
                if (name.StartsWith("w", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[1..], out var id))
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
                        .FirstOrDefault(x => name.Split(' ')
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
                
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = character.Name
                    }.AddField(GetText(Localization.CommonId), $"w{character.CharacterId}", true)
                    .AddField(GetText(Localization.SearchesSource), GetText(Localization.BotDatabase), true)
                    .AddField(GetText(Localization.SearchesDescription), !string.IsNullOrEmpty(character.Description) ? $"{character.Description}" : "-")
                    .WithImageUrl(character.ImageUrl);

                await ReplyAsync(embed);
            }

            [Command("animelist")]
            [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task AnimeListAsync([Remainder] string title)
            {
                var animeList = await Service.GetAniListInfoAsync<List<AnimeMangaContent>>(AnimeService.AnimeListQuery, new { anime = title }, "Page", "media");
                if (animeList is null || animeList.Count == 0)
                {
                    await ReplyErrorAsync(Localization.SearchesAnimeListNotFound);
                    return;
                }

                await SendPaginatedMessageAsync(animeList, 10, (items, index) => new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.SearchesAnimeList, title, Context.Prefix),
                    Description = string.Join("\n", items.Select(c =>
                        $"[{(string.IsNullOrEmpty(c.Title.Romaji) ? c.Title.English : c.Title.Romaji)}]({c.SiteUrl}) ({c.Id})"))
                });
            }

            [Command("mangalist")]
            [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task MangaListAsync([Remainder] string title)
            {
                var mangaList = await Service.GetAniListInfoAsync<List<AnimeMangaContent>>(AnimeService.MangaListQuery, new { manga = title }, "Page", "media");
                if (mangaList is null || mangaList.Count == 0)
                {
                    await ReplyErrorAsync(Localization.SearchesMangaListNotFound);
                    return;
                }
                
                await SendPaginatedMessageAsync(mangaList, 10, (items, index) => new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.SearchesMangaList, title, Context.Prefix),
                    Description = string.Join("\n", items.Select(c =>
                        $"[{(string.IsNullOrEmpty(c.Title.Romaji) ? c.Title.English : c.Title.Romaji)}]({c.SiteUrl}) ({c.Id})"))
                });
            }

            [Command("characters")]
            [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task CharactersAsync([Remainder] string name)
            {
                var characters = DbContext.CustomCharacters
                    .AsEnumerable()
                    .Where(x => name.Split(' ')
                        .All(y => x.Name!.Contains(y, StringComparison.InvariantCultureIgnoreCase)))
                    .ToList<object>();

                var anilistCharacters = await Service.GetAniListInfoAsync<List<CharacterContent>>(AnimeService.CharacterListQuery, new { character = name }, "Page", "characters");
                if (characters.Count == 0 && (anilistCharacters is null || anilistCharacters.Count == 0))
                {
                    await ReplyErrorAsync(Localization.SearchesCharactersNotFound);
                    return;
                }

                if (anilistCharacters != null)
                    characters.AddRange(anilistCharacters);

                await SendPaginatedMessageAsync(characters, 10, (items, index) => new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.SearchesCharacterList, name, Context.Prefix),
                    Description = string.Join("\n", items.Select(c =>
                    {
                        if (c is CustomCharactersEntity customCharacter)
                            return $"• {customCharacter.Name} (w{customCharacter.CharacterId})";

                        var character = (CharacterContent)c;
                        var fromAnime = GetCharacterSources(character, "anime").FirstOrDefault();
                        var from = fromAnime != null
                            ? $"{GetText(Localization.SearchesFromAnime)}: {fromAnime}"
                            : $"{GetText(Localization.SearchesFromAnime)}: {GetCharacterSources(character, "manga").FirstOrDefault()}";
                        
                        return $"• [{character.Name.First} {character.Name.Last}]({character.SiteUrl}) ({character.Id}) | {from}";
                    }))
                });
            }
            
            private async Task AniListCharacterAsync(string name)
            {
                var (type, method) = int.TryParse(name, out _) ? ("Int", "id") : ("String", "search");
                var query = AnimeService.CharacterQuery.Replace("[type]", type)
                    .Replace("[var]", method);
            
                var character = await Service.GetAniListInfoAsync<CharacterContent>(query, new { character = name }, "Character");

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
                var mangaSb = new StringBuilder(900);
                if (mangaList.Count != 0)
                {
                    foreach (var manga in mangaList)
                    {
                        if (mangaSb.Length + manga.Length > 900)
                        {
                            mangaSb.Append($"[{GetText(Localization.More).ToLowerInvariant()}]({character.SiteUrl})");
                            break;
                        }
                        
                        mangaSb.Append(manga).AppendLine();
                    }
                }
                else
                {
                    mangaSb.Append('-');
                }

                var animeList = GetCharacterSources(character, "anime");
                var animeSb = new StringBuilder(900);
                if (animeList.Count != 0)
                {
                    foreach (var anime in animeList)
                    {
                        if (animeSb.Length + anime.Length > 900)
                        {
                            animeSb.Append($"[{GetText(Localization.More).ToLowerInvariant()}]({character.SiteUrl})");
                            break;
                        }
                        
                        animeSb.Append(anime).AppendLine();
                    }
                }
                else
                {
                    animeSb.Append('-');
                }

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Url = character.SiteUrl,
                    Title = $"{character.Name.First} {character.Name.Last}"
                }.AddField(GetText(Localization.SearchesFirstName), !string.IsNullOrEmpty(character.Name.First) ? character.Name.First : "-", true)
                    .AddField(GetText(Localization.SearchesLastName), !string.IsNullOrEmpty(character.Name.Last) ? character.Name.Last : "-", true)
                    .AddField(GetText(Localization.SearchesNativeName), !string.IsNullOrEmpty(character.Name.Native) ? character.Name.Native : "-", true)
                    .AddField(GetText(Localization.SearchesAlternative), alternative, true)
                    .AddField(GetText(Localization.CommonId), character.Id.ToString(), true)
                    .AddField(GetText(Localization.SearchesFavourites), character.Favourites.HasValue ? character.Favourites.Value.ToString() : "-", true)
                    .AddField(GetText(Localization.SearchesFromManga), mangaSb.ToString(), true)
                    .AddField(GetText(Localization.SearchesFromAnime), animeSb.ToString(), true)
                    .AddField(GetText(Localization.SearchesSource), "AniList", true)
                    .AddField(GetText(Localization.SearchesDescription), !string.IsNullOrEmpty(character.Description)
                        ? $"{character.Description.Truncate(900)} [{GetText(Localization.More).ToLowerInvariant()}]({character.SiteUrl})"
                        : "-")
                    .WithImageUrl(character.Image.Large);

                await ReplyAsync(embed);
            }

            private IList<string> GetCharacterSources(CharacterContent character, string from)
                => character.Media.Nodes!
                    .Where(x => string.Equals(x.Type, from, StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"[{(string.IsNullOrEmpty(x.Title.Romaji) ? x.Title.English : x.Title.Romaji)}]({x.SiteUrl}) ({x.Id})")
                    .ToList();
        }
    }
}