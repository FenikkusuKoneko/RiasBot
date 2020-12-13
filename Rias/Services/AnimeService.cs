using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rias.Database;
using Rias.Database.Entities;
using Rias.Services.Commons;

namespace Rias.Services
{
    public class AnimeService : RiasService
    {
        private const string AniListGraphQlUrl = "https://graphql.anilist.co";
        private readonly HttpClient _httpClient;
        
        public AnimeService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }
        
        public async Task<ICharacterEntity?> GetOrAddCharacterAsync(string name)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            
            if (name.StartsWith("w", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[1..], out var id))
                return await db.CustomCharacters.FirstOrDefaultAsync(x => x.CharacterId == id);

            var characterDb = int.TryParse(name, out id)
                ? await db.Characters.FirstOrDefaultAsync(x => x.CharacterId == id)
                : db.CustomCharacters
                      .AsEnumerable()
                      .FirstOrDefault(x =>
                          name.Split(' ')
                              .All(y => x.Name!.Contains(y, StringComparison.InvariantCultureIgnoreCase)))
                  ?? (ICharacterEntity?) db.Characters
                      .AsEnumerable()
                      .FirstOrDefault(x =>
                          name.Split(' ')
                              .All(y => x.Name!.Contains(y, StringComparison.InvariantCultureIgnoreCase)));

            if (characterDb is null)
            {
                var aniListCharacter = id > 0
                    ? await GetAniListCharacterById(id)
                    : await GetAniListCharacterByName(name);
                
                if (aniListCharacter is null)
                    return null;
                
                characterDb = await db.Characters.FirstOrDefaultAsync(x => x.CharacterId == aniListCharacter.Id);
                if (characterDb is null)
                {
                    var newCharacterDb = new CharacterEntity
                    {
                        CharacterId = aniListCharacter.Id,
                        Name = $"{aniListCharacter.Name.First} {aniListCharacter.Name.Last}".Trim(),
                        Url = aniListCharacter.SiteUrl,
                        ImageUrl = aniListCharacter.Image.Large
                    };
                    
                    await db.AddAsync(newCharacterDb);
                    await db.SaveChangesAsync();
                
                    return newCharacterDb;
                }
            }

            if (characterDb is CustomCharacterEntity)
                return characterDb;

            if (!await CheckCharacterImageAsync(characterDb.ImageUrl!))
            {
                var aniListCharacter = await GetAniListCharacterById(characterDb.CharacterId);
                if (aniListCharacter is null)
                    return null;

                var characterImage = aniListCharacter.Image.Large;
                if (!string.IsNullOrEmpty(characterImage))
                {
                    characterDb.ImageUrl = characterImage;
                    await SetCharacterImageUrlAsync(aniListCharacter.Id, characterImage);
                }
            }

            return characterDb;
        }
        
        public async Task<TType?> GetAniListInfoAsync<TType>(string query, object variables, params string[] tokens)
            where TType : class
        {
            var graphQlQuery = new GraphQlQuery
            {
                Query = query,
                Variables = variables   
            };
            
            var queryJson = JsonConvert.SerializeObject(graphQlQuery);
            using var response = await _httpClient.PostAsync(AniListGraphQlUrl, new StringContent(queryJson, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
                return null;

            return JObject.Parse(await response.Content.ReadAsStringAsync())
                .SelectToken($"data.{string.Join(".", tokens)}")?
                .ToObject<TType>();
        }
        
        public async Task SetCharacterImageUrlAsync(int id, string url)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var character = await db.Characters.FirstOrDefaultAsync(x => x.CharacterId == id);
            if (character != null)
            {
                character.ImageUrl = url;
                await db.SaveChangesAsync();
            }
        }
        
        public Task<CharacterContent?> GetAniListCharacterById(int id)
        {
            var query = CharacterQuery.Replace("[type]", "Int").Replace("[var]", "id");
            return GetAniListInfoAsync<CharacterContent>(query, new { character = id }, "Character");
        }
        
        private Task<CharacterContent?> GetAniListCharacterByName(string name)
        {
            var query = CharacterQuery.Replace("[type]", "String").Replace("[var]", "search");
            return GetAniListInfoAsync<CharacterContent>(query, new { character = name }, "Character");
        }
        
        private async Task<bool> CheckCharacterImageAsync(string characterUrl)
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var response = await _httpClient.GetAsync(characterUrl, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        private struct GraphQlQuery
        {
            [JsonProperty("query")]
            public string Query { get; set; }
            
            [JsonProperty("variables")]
            public object Variables { get; set; }
        }

        public const string AnimeQuery =
            @"query ($anime: [type]) {
              Media([var]: $anime, type: ANIME) {
                id
                siteUrl
                title {
                  romaji
                  english
                  native
                }
                format
                episodes
                duration
                status
                startDate {
                  year
                  month
                  day
                }
                endDate {
                  year
                  month
                  day
                }
                season
                averageScore
                meanScore
                popularity
                favourites
                source
                genres
                isAdult
                description
                coverImage {
                  large
                }
              }
            }";
        
        public const string MangaQuery = 
            @"query ($manga: [type]) {
                Media([var]: $manga, type: MANGA) {
                  id
                  siteUrl
                  title {
                    romaji
                    english
                    native
                  }
                  format
                  chapters
                  volumes
                  status
                  startDate {
                    year
                    month
                    day
                  }
                  endDate {
                    year
                    month
                    day
                  }
                  averageScore
                  meanScore
                  popularity
                  favourites
                  source
                  genres
                  synonyms
                  isAdult
                  description
                  coverImage {
                    large
                  }
                }
            }";

        public const string CharacterQuery =
            @"query ($character: [type]) {
                Character([var]: $character) {
                  id
                  siteUrl
                  name {
                    first
                    last
                    native
                    alternative
                  }
                  favourites
                  media {
                    nodes {
                      id
                      siteUrl
                      title {
                        romaji
                        english
                        native
                      }
                      type
                    }
                  }
                  description
                  image {
                    large
                  }
                }
            }";
        
        public const string AnimeListQuery =
            @"query ($anime: String) {
              Page {
                media(search: $anime, type: ANIME) {
                  id
                  siteUrl
                  title {
                    romaji
                    english
                    native
                  }
                }
              }
            }";
        
        public const string MangaListQuery =
            @"query ($manga: String) {
              Page {
                media(search: $manga, type: MANGA) {
                  id
                  siteUrl
                  title {
                    romaji
                    english
                    native
                  }
                }
              }
            }";
        
        public const string CharacterListQuery =
            @"query ($character: String) {
              Page {
                characters(search: $character) {
                  id
                  siteUrl
                  name {
                    first
                    last
                    native
                    alternative
                  }
                  media {
                    nodes {
                      id
                      siteUrl
                      title {
                        romaji
                        english
                        native
                      }
                      type
                    }
                  }
                }
              }
            }";
    }
}