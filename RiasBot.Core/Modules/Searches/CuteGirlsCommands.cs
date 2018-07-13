using Discord;
using Discord.Commands;
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
            [RequireContext(ContextType.Guild)]
            [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
            public async Task Neko()
            {
                var neko = await _service.GetNekoImage();
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle("Neko <3");
                embed.WithImageUrl(neko);

                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
            public async Task Kitsune()
            {
                var kitsune = await _service.GetKitsuneImage();
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle("Kitsune <3");
                embed.WithImageUrl(kitsune);

                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
        }
    }
}
