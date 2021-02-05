using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Rias.Services
{
    public class ReactionsService : RiasService
    {
        private readonly HttpClient _httpClient;
        
        public ReactionsService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Wolke {Configuration.WeebServicesToken}");
        }

        public void AddWeebUserAgent(string weebUserAgent)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                return;
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", weebUserAgent);
        }
        
        public async Task<string?> GetReactionAsync(string type)
        {
            using var response = await _httpClient.GetAsync($"https://api-v3.weeb.sh/images/random?type={type}&filetype=gif");
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync())!["url"].ToString();
        }

        public async Task<string?> GetImageAsync(string type)
        {
            using var response = await _httpClient.GetAsync($"https://rias.gg/api/images?type={type}");
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())!["url"];
        }
    }
}