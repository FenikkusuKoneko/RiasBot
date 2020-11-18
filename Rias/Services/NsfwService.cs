using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Attributes;
using Serilog;

namespace Rias.Services
{
    [AutoStart]
    public class NsfwService : RiasService
    {
        private const string DanbooruApi = "https://danbooru.donmai.us/posts.json?limit=100&tags=rating:explicit+";
        private const string KonachanApi = "https://konachan.com/post.json?s=post&q=index&limit=100&tags=rating:explicit+";
        private const string YandereApi = "https://yande.re/post.json?limit=100&tags=rating:explicit+";
        private const string GelbooruApi = "https://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags=rating:explicit+";
        
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly HashSet<string> _downloadedTags = new HashSet<string>();
        private readonly HttpClient _httpClient;

        private readonly ImmutableHashSet<string> _blacklistTags = ImmutableHashSet.Create("loli", "shota", "cub");
        
        public NsfwService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            RunTaskAsync(InitializeAsync);
        }
        
        public enum NsfwImageApiProvider
        {
            Danbooru = 0,
            Konachan = 1,
            Yandere = 2,
            Gelbooru = 3,
            Random = 4
        }
        
        public bool CacheInitialized { get; private set; }

        public async Task<NsfwImage?> GetNsfwImageAsync(NsfwImageApiProvider provider, string? tags = null)
        {
            tags = tags?.ToLower();
            var random = new Random();
            if (provider == NsfwImageApiProvider.Random)
                provider = (NsfwImageApiProvider)random.Next(4);

            var nsfwImages = _cache.Get<HashSet<NsfwImage>>(provider);
            List<NsfwImage> nsfwImagesList;

            if (string.IsNullOrEmpty(tags))
            {
                nsfwImagesList = nsfwImages.ToList();
                return nsfwImagesList[random.Next(nsfwImagesList.Count)];
            }

            var tagsList = tags.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().Replace(" ", "_"))
                .Where(y => !_blacklistTags.Contains(y))
                .ToList();

            if (tagsList.Count == 0 || tagsList.Count > 3)
                return null;

            foreach (var tag in tagsList.Where(tag => !_downloadedTags.Contains($"{provider.ToString()}_{tag}")))
            {
                await PopulateCacheTagAsync(provider, tag);
            }

            nsfwImagesList = nsfwImages.Where(x => tagsList.All(tag1 => x.Tags!.Any(tag2 => string.Equals(tag1, tag2)))).ToList();
            
            if (nsfwImagesList.Count == 0)
                return null;
            
            return nsfwImagesList[random.Next(nsfwImagesList.Count)];
        }
        
        private async Task InitializeAsync()
        {
            var danbooruImages = await DeserializeJsonHentaiAsync(DanbooruApi);
            await PopulateCacheAsync(danbooruImages, NsfwImageApiProvider.Danbooru);
            var konachanImages = await DeserializeJsonHentaiAsync(KonachanApi);
            await PopulateCacheAsync(konachanImages, NsfwImageApiProvider.Konachan);
            var yandereImages = await DeserializeJsonHentaiAsync(YandereApi);
            await PopulateCacheAsync(yandereImages, NsfwImageApiProvider.Yandere);
            var gelbooruImages = await DeserializeXmlHentaiAsync(GelbooruApi);
            await PopulateCacheAsync(gelbooruImages, NsfwImageApiProvider.Gelbooru);

            CacheInitialized = true;
        }
        
        private async Task PopulateCacheAsync(IList<NsfwImageApi>? nsfwImagesApi, NsfwImageApiProvider provider)
        {
            if (nsfwImagesApi is null)
                return;

            var imagesApi = new List<NsfwImage>();
            foreach (var nsfwImageApi in nsfwImagesApi)
            {
                var imageTags = string.IsNullOrEmpty(nsfwImageApi.Tags) ? nsfwImageApi.TagString! : nsfwImageApi.Tags;
                var tags = imageTags!.ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (tags.Any(x => _blacklistTags.Contains(x)))
                    continue;
                
                imagesApi.Add(new NsfwImage
                {
                    Url = nsfwImageApi.FileUrl,
                    Tags = tags.ToList(),
                    Provider = provider
                });
            }

            foreach (var image in imagesApi)
            {
                var nsfwList = await _cache.GetOrCreateAsync(provider, _ => Task.FromResult(new HashSet<NsfwImage>()));
                if (!nsfwList.Contains(image))
                    nsfwList.Add(image);
            }

            Log.Debug($"{provider} NSFW images cached");
        }
        
        private async Task PopulateCacheTagAsync(NsfwImageApiProvider provider, string tag)
        {
            var url = provider switch
            {
                NsfwImageApiProvider.Danbooru => DanbooruApi,
                NsfwImageApiProvider.Konachan => KonachanApi,
                NsfwImageApiProvider.Yandere => YandereApi,
                NsfwImageApiProvider.Gelbooru => GelbooruApi,
                _ => null
            };

            _downloadedTags.Add($"{provider.ToString()}_{tag}");

            var nsfwImages = provider != NsfwImageApiProvider.Gelbooru
                ? await DeserializeJsonHentaiAsync(url + tag)
                : await DeserializeXmlHentaiAsync(url + tag);

            await PopulateCacheAsync(nsfwImages, provider);
            
            Log.Debug($"NSFW tag <{tag}> downloaded");
        }
        
        private async Task<IList<NsfwImageApi>?> DeserializeJsonHentaiAsync(string url)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<NsfwImageApi>>(result)
                    .Where(x => !string.IsNullOrEmpty(x.FileUrl))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }
        
        private async Task<IList<NsfwImageApi>?> DeserializeXmlHentaiAsync(string url)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
            
                var nsfwImages = new List<NsfwImageApi>();
                while (await reader.ReadAsync())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "post")
                    {
                        var fileUrl = reader["file_url"];
                        if (string.IsNullOrEmpty(fileUrl))
                            continue;
                        
                        nsfwImages.Add(new NsfwImageApi
                        {
                            FileUrl = fileUrl,
                            Tags = reader["tags"]
                        });
                    }
                }

                return nsfwImages;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        public class NsfwImage
        {
            public string? Url { get; set; }
            
            public IList<string>? Tags { get; set; }
            
            public NsfwImageApiProvider? Provider { get; set; }
        }
        
        private class NsfwImageApi
        {
            [JsonProperty("file_url")]
            public string? FileUrl { get; set; }
            
            [JsonProperty("tags")]
            public string? Tags { get; set; }
            
            [JsonProperty("tag_string")]
            public string? TagString { get; set; }
        }
    }
}