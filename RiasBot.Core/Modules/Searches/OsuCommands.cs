using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Searches
{
    public partial class Searches
    {
        public class OsuCommands : RiasSubmodule
        {
            public OsuCommands()
            {

            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Osu(string user)
            {
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

                try
                {
                    var http = new HttpClient();
                    var res = await http.GetStreamAsync(new Uri($"http://lemmmy.pw/osusig/sig.php?colour=hexf50057&uname={user}&mode=0&pp=2&countryrank&removeavmargin&flagshadow&darktriangles&opaqueavatar&onlineindicator=undefined&xpbar&xpbarhex"));
                    var ms = new MemoryStream();
                    res.CopyTo(ms);
                    ms.Position = 0;
                    await Context.Channel.SendFileAsync(ms, $"{user}.png").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find anything.");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Mania(string user)
            {
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

                try
                {
                    var http = new HttpClient();
                    var res = await http.GetStreamAsync(new Uri($"http://lemmmy.pw/osusig/sig.php?colour=hexf50057&uname={user}&mode=3&pp=2&countryrank&removeavmargin&flagshadow&darktriangles&opaqueavatar&onlineindicator=undefined&xpbar&xpbarhex"));
                    var ms = new MemoryStream();
                    res.CopyTo(ms);
                    ms.Position = 0;
                    await Context.Channel.SendFileAsync(ms, $"{user}.png").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find anything.");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Taiko(string user)
            {
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

                try
                {
                    var http = new HttpClient();
                    var res = await http.GetStreamAsync(new Uri($"http://lemmmy.pw/osusig/sig.php?colour=hexf50057&uname={user}&mode=1&pp=2&countryrank&removeavmargin&flagshadow&darktriangles&opaqueavatar&onlineindicator=undefined&xpbar&xpbarhex"));
                    var ms = new MemoryStream();
                    res.CopyTo(ms);
                    ms.Position = 0;
                    await Context.Channel.SendFileAsync(ms, $"{user}.png").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find anything.");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Ctb(string user)
            {
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

                try
                {
                    var http = new HttpClient();
                    var res = await http.GetStreamAsync(new Uri($"http://lemmmy.pw/osusig/sig.php?colour=hexf50057&uname={user}&mode=2&pp=2&countryrank&removeavmargin&flagshadow&darktriangles&opaqueavatar&onlineindicator=undefined&xpbar&xpbarhex"));
                    var ms = new MemoryStream();
                    res.CopyTo(ms);
                    ms.Position = 0;
                    await Context.Channel.SendFileAsync(ms, $"{user}.png").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find anything.");
                }
            }
        }
    }
}
