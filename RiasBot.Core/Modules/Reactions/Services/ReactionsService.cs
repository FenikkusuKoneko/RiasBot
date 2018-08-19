using Newtonsoft.Json;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Reactions.Services
{
    public class ReactionsService : IRService
    {
        private readonly IBotCredentials _creds;

        public ReactionsService(IBotCredentials creds)
        {
            _creds = creds;
        }

        public async Task<string> GetPatImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var patRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=pat&filetype=gif").ConfigureAwait(false);
                if (patRequest.IsSuccessStatusCode)
                {
                    var patImage = JsonConvert.DeserializeObject<WeebServices>(await patRequest.Content.ReadAsStringAsync());
                    return patImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetHugImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var hugRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=hug&filetype=gif").ConfigureAwait(false);
                if (hugRequest.IsSuccessStatusCode)
                {
                    var hugImage = JsonConvert.DeserializeObject<WeebServices>(await hugRequest.Content.ReadAsStringAsync());
                    return hugImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetKissImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var kissRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=kiss&filetype=gif").ConfigureAwait(false);
                if (kissRequest.IsSuccessStatusCode)
                {
                    var kissImage = JsonConvert.DeserializeObject<WeebServices>(await kissRequest.Content.ReadAsStringAsync());
                    return kissImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetBiteImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var biteRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=bite&filetype=gif").ConfigureAwait(false);
                if (biteRequest.IsSuccessStatusCode)
                {
                    var biteImage = JsonConvert.DeserializeObject<WeebServices>(await biteRequest.Content.ReadAsStringAsync());
                    return biteImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetLickImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var lickRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=lick&filetype=gif").ConfigureAwait(false);
                if (lickRequest.IsSuccessStatusCode)
                {
                    var lickImage = JsonConvert.DeserializeObject<WeebServices>(await lickRequest.Content.ReadAsStringAsync());
                    return lickImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetCryImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var cryRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=cry&filetype=gif").ConfigureAwait(false);
                if (cryRequest.IsSuccessStatusCode)
                {
                    var cryImage = JsonConvert.DeserializeObject<WeebServices>(await cryRequest.Content.ReadAsStringAsync());
                    return cryImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetCuddleImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var cuddleRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=cuddle&filetype=gif").ConfigureAwait(false);
                if (cuddleRequest.IsSuccessStatusCode)
                {
                    var cuddleImage = JsonConvert.DeserializeObject<WeebServices>(await cuddleRequest.Content.ReadAsStringAsync());
                    return cuddleImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetSlapImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Wolke " + _creds.WeebServicesToken);
                http.DefaultRequestHeaders.Add("User-Agent", "RiasBot/" + RiasBot.Version);
                var slapRequest = await http.GetAsync(RiasBot.WeebApi + "images/random?type=slap&filetype=gif").ConfigureAwait(false);
                if (slapRequest.IsSuccessStatusCode)
                {
                    var slapImage = JsonConvert.DeserializeObject<WeebServices>(await slapRequest.Content.ReadAsStringAsync());
                    return slapImage.Url;
                }

                return null;
            }
        }
        
        public async Task<string> GetGropeImage()
        {
            using (var http = new HttpClient())
            {
                var kitsuneRequest = await http.GetAsync(RiasBot.Website + "api/grope").ConfigureAwait(false);
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