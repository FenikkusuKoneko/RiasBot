using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Rias.Core.Services
{
    public class CuteGirlsService : RiasService
    {
        private readonly HttpClient _httpClient;

        private const string RiasWebsite = "https://riasbot.me";
        
        public CuteGirlsService(IServiceProvider services) : base(services)
        {
            _httpClient = services.GetRequiredService<HttpClient>();
        }

        public async Task<string?> GetNekoImageAsync()
        {
            using var response = await _httpClient.GetAsync(RiasWebsite + "/api/neko");
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())["url"];
        }
        
        public async Task<string?> GetKitsuneImageAsync()
        {
            using var response = await _httpClient.GetAsync(RiasWebsite + "/api/kitsune");
            if (!response.IsSuccessStatusCode)
                return null;

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())["url"];
        }
    }
}