using Newtonsoft.Json;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Searches.Services
{
    public class CuteGirlsService : IRService
    {
        private readonly IBotCredentials _creds;
        public CuteGirlsService(IBotCredentials creds)
        {
            _creds = creds;
        }

        public async Task<string> GetNekoImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var patRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=neko&filetype=gif").ConfigureAwait(false);
                if (patRequest.IsSuccessStatusCode)
                {
                    var patImage = JsonConvert.DeserializeObject<WeebServices>(await patRequest.Content.ReadAsStringAsync());
                    return patImage.Url;
                }

                return null;
            }
        }

        public async Task<string> GetKitsuneImage()
        {
            using (var http = new HttpClient())
            {
                var kitsuneRequest = await http.GetAsync(RiasBot.Website + "api/kitsune").ConfigureAwait(false);
                if (kitsuneRequest.IsSuccessStatusCode)
                {
                    var kitsuneImage = JsonConvert.DeserializeObject<Dictionary<string, string>>(await kitsuneRequest.Content.ReadAsStringAsync());
                    return kitsuneImage["url"];
                }

                return null;
            }
        }
        
        private class WeebServices
        {
            //public int Status { get; }
            //public string Id { get; }
            //public string Type { get; }
            //public string BaseType { get; }
            //public bool Nsfw { get; }
            //public string FileType { get; }
            //public string MimeType { get; }
            //public string Account { get; }
            //public bool Hidden { get; }
            //public string[] Tags { get; }
            public string Url { get; set; }
        }
    }
}
