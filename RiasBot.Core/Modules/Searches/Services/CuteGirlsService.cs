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
        public CuteGirlsService()
        {

        }

        public async Task<string> GetNekoImage()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    var nekoUrl = await http.GetStringAsync(RiasBot.Website + "api/neko").ConfigureAwait(false);
                    var nekoImage = JsonConvert.DeserializeObject<Dictionary<string, string>>(nekoUrl);
                    return nekoImage["url"];
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetKitsuneImage()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    var kitsuneUrl = await http.GetStringAsync(RiasBot.Website + "api/kitsune").ConfigureAwait(false);
                    var kitsuneImage = JsonConvert.DeserializeObject<Dictionary<string, string>>(kitsuneUrl);
                    return kitsuneImage["url"];
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
