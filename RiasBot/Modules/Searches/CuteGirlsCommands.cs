using Discord;
using Newtonsoft.Json.Linq;
using RiasBot.Commons.Attributes;
using RiasBot.Modules.Searches.Services;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Searches
{
    public partial class Searches
    {
        public class CuteGirlsCommands : RiasSubmodule<CuteGirlsService>
        {
            private readonly CommandHandler _ch;
            private readonly IBotCredentials _creds;

            public CuteGirlsCommands(CommandHandler ch, IBotCredentials creds)
            {
                _ch = ch;
                _creds = creds;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Neko()
            {
                string nekoURL = null;

                using (var http = new HttpClient())
                {
                    nekoURL = await http.GetStringAsync("https://nekos.life/api/neko");
                }

                var getNeko = JObject.Parse(nekoURL);
                var neko = getNeko["neko"];

                var embed = new EmbedBuilder();
                embed.WithColor(RiasBot.goodColor);
                embed.WithTitle("Neko <3");
                embed.WithImageUrl((string)neko);

                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Kitsune()
            {
                var kitsune = await _service.GetKitsuneImage().ConfigureAwait(false);
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle("Kitsune <3");
                embed.WithImageUrl(kitsune);

                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
        }
    }
}
