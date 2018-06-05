using Discord;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Searches.Services;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Searches
{
    public partial class Searches
    {
        public class GoogleCommands : RiasSubmodule<GoogleService>
        {
            private readonly CommandHandler _ch;

            public GoogleCommands(CommandHandler ch)
            {
                _ch = ch;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task YouTube(string type, [Remainder]string keywords)
            {
                string[] searches = null;
                try
                {
                    searches = (await _service.YouTubeSearch(type, keywords).ConfigureAwait(false)).Where(x => !String.IsNullOrEmpty(x)).ToArray();
                }
                catch
                {

                }

                Random r = new Random();
                int random = r.Next(0, searches?.Length ?? 0);

                switch (type)
                {
                    case "video":
                        if (searches.Length > 0)
                            await Context.Channel.SendMessageAsync("https://youtu.be/" + searches[random]).ConfigureAwait(false);
                        else
                            await Context.Channel.SendErrorEmbed("I couldn't find any video.").ConfigureAwait(false);
                        break;
                    case "channel":
                        if (searches.Length > 0)
                            await Context.Channel.SendMessageAsync("https://www.youtube.com/channel/" + searches[0]).ConfigureAwait(false);
                        else
                            await Context.Channel.SendErrorEmbed("I couldn't find any channel.").ConfigureAwait(false);
                        break;
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task YouTubeList([Remainder]string keywords)
            {
                var searches = await _service.YouTubeSearch("videolist", keywords).ConfigureAwait(false);
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle("YouTube Search");

                for (int i = 0; i < searches.Length; i++)
                {
                    if (i >= 10)
                        break;
                    var video = searches[i].Split("&id=");
                    embed.AddField($"#{i+1} {video[0]}", $"https://youtu.be/{video[1]}");
                }
                if (searches.Length == 0)
                    embed.WithDescription("I couldn't find anything");

                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Google([Remainder]string keywords)
            {
                var searches = await _service.GoogleSearch(keywords).ConfigureAwait(false);
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle($"Google Search: {keywords}");

                if (searches != null)
                {
                    for (int i = 0; i < searches.Length; i++)
                    {
                        var search = searches[i].Split("&link=");
                        embed.AddField($"#{i + 1} {search[0]}", search[1]);
                    }
                    await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find anything");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Image([Remainder]string keywords)
            {
                var rand = new Random((int)DateTime.UtcNow.Ticks);
                var searches = await _service.GoogleImageSearch(keywords).ConfigureAwait(false);
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle($"Google Image Search: {keywords}");

                if (searches != null)
                {
                    embed.WithImageUrl(searches[rand.Next(0, searches.Length)]);
                    await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find anything");
                }
            }
        }
    }
}
