using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Rias.Core.Services
{
    public class ReactionsService : RiasService
    {
        private readonly HttpClient _httpClient;
        
        public ReactionsService(IServiceProvider services) : base(services)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Wolke " + Creds.WeebServicesToken);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"Rias v.{Rias.Version}");
        }

        public async Task<string?> GetReactionUrlAsync(string type)
        {
            using var response = await _httpClient.GetAsync($"https://api-v2.weeb.sh/images/random?type={type}&filetype=gif");
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync())["url"].ToString();
        }

        public async Task<string?> GetGropeUrlAsync()
        {
            using var response = await _httpClient.GetAsync("https://riasbot.me/api/grope");
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())["url"];
        }
    }
}