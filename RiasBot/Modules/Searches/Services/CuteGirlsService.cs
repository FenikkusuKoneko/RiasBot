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
            UpdateNekos().GetAwaiter().GetResult();
            UpdateKitsunes().GetAwaiter().GetResult();
        }

        private List<string> nekoList = new List<string>();
        private List<string> kitsuneList = new List<string>();

        public string GetNekoImage()
        {
            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            int nekoIndex = rnd.Next(0, nekoList.Count);
            return nekoList[nekoIndex];
        }

        public async Task UpdateNekos()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Client-ID " + _creds.ImgurClientID);

                var url = "https://api.imgur.com/3/album/XjyX9/images"; //my album with neko girls
                var data = await http.GetStringAsync(url);

                var nekos = JsonConvert.DeserializeObject<NekoKitsuneData>(data);

                foreach (var neko in nekos.data)
                {
                    if (!nekoList.Contains(neko.link))
                        nekoList.Add(neko.link);
                }
            }
        }

        public string GetKitsuneImage()
        {
            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            int kitsuneIndex = rnd.Next(0, kitsuneList.Count);
            return kitsuneList[kitsuneIndex];
        }

        public async Task UpdateKitsunes()
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", "Client-ID " + _creds.ImgurClientID);

                var url = "https://api.imgur.com/3/album/aBaoM/images"; //my album with neko girls
                var data = await http.GetStringAsync(url);

                var kitsunes = JsonConvert.DeserializeObject<NekoKitsuneData>(data);

                foreach (var kitsune in kitsunes.data)
                {
                    if (!kitsuneList.Contains(kitsune.link))
                        kitsuneList.Add(kitsune.link);
                }
            }
        }
    }

    public class NekoKitsuneData
    {
        public List<NekoKitsune> data { get; set; }
    }

    public class NekoKitsune
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
