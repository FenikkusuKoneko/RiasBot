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
            UpdateImages("H1Pqa", biteList).GetAwaiter().GetResult();
            UpdateImages("woGOn", cryList).GetAwaiter().GetResult();
            UpdateImages("Xqjh9UM", cuddleList).GetAwaiter().GetResult();
            UpdateImages("GdiXR", gropeList).GetAwaiter().GetResult();
            UpdateImages("KTkPe", hugList).GetAwaiter().GetResult();
            UpdateImages("CotHR", kissList).GetAwaiter().GetResult();
            UpdateImages("5cMDN", lickList).GetAwaiter().GetResult();
            UpdateImages("OQjWy", patList).GetAwaiter().GetResult();
            UpdateImages("AQoU8", slapList).GetAwaiter().GetResult();
        }

        public List<string> biteList = new List<string>();
        public List<string> cryList = new List<string>();
        public List<string> cuddleList = new List<string>();
        public List<string> gropeList = new List<string>();
        public List<string> hugList = new List<string>();
        public List<string> kissList = new List<string>();
        public List<string> lickList = new List<string>();
        public List<string> patList = new List<string>();
        public List<string> slapList = new List<string>();

        public string GetImage(List<string> listImages)
        {
            try
            {
                var rnd = new Random((int)DateTime.UtcNow.Ticks);
                var index = rnd.Next(0, listImages.Count);
                return listImages[index];
            }
            catch
            {
                return null;
            }
        }

        public async Task UpdateImages(string albumID, List<string> listImages)
        {
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Clear();
                    http.DefaultRequestHeaders.Add("Authorization", "Client-ID " + _creds.ImgurClientID);

                    var url = $"https://api.imgur.com/3/album/{albumID}/images";
                    var data = await http.GetStringAsync(url);

                    var images = JsonConvert.DeserializeObject<ImgurData>(data);

                    foreach (var image in images.data)
                    {
                        if (!listImages.Contains(image.link))
                            listImages.Add(image.link);
                    }
                }
            }
            catch
            {

            }
        }
    }

    public class ImgurData
    {
        public List<ImgurImage> data { get; set; }
    }

    public class ImgurImage
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