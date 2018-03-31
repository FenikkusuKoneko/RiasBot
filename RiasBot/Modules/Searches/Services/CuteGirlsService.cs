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

        public async Task<string> GetKitsuneImage()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Client-ID " + _creds.ImgurClientID);

                var url = "https://api.imgur.com/3/album/aBaoM/images"; //my album with kitsune girls
                var data = await http.GetStringAsync(url);

                var kitsunes = JsonConvert.DeserializeObject<KitsuneData>(data);

                var rnd = new Random((int)DateTime.UtcNow.Ticks);
                int kitsuneIndex = rnd.Next(0, kitsunes.data.Count);
                return kitsunes.data[kitsuneIndex].link;
            }
        }
    }

    public class KitsuneData
    {
        public List<Kitsune> data { get; set; }
    }

    public class Kitsune
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        /// <summary>
        /// The DateTimne is represented in ticks
        /// </summary>
        public long datetime { get; set; }
        /// <summary>
        /// The type of the image, png, jpg or jpeg. The result is image/jpeg for example
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// If the image is a gif or not
        /// </summary>
        public bool animated { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        /// <summary>
        /// The site in bytes of the image
        /// </summary>
        public int size { get; set; }
        public int views { get; set; }
        public object nsfw { get; set; }
        public string[] tags { get; set; }
        public string deletehash { get; set; }
        public string name { get; set; }
        public string link { get; set; }
    }
}
