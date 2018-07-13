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

        private List<string> kitsuneList = new List<string>();

        public async Task<string> GetNekoImage()
        {
            try
            {
                using (var http = new HttpClient())
                {
                    string nekoUrl = await http.GetStringAsync(RiasBot.Website + "neko").ConfigureAwait(false);
                    var nekoImage = JsonConvert.DeserializeObject<Dictionary<string, string>>(nekoUrl);
                    return nekoImage["neko"];
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
                    string nekoUrl = await http.GetStringAsync(RiasBot.Website + "kitsune").ConfigureAwait(false);
                    var nekoImage = JsonConvert.DeserializeObject<Dictionary<string, string>>(nekoUrl);
                    return nekoImage["kitsune"];
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
