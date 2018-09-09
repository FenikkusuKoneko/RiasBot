using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.NSFW.Services;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.NSFW
{
    public class NSFW : RiasModule<NSFWService>
    {
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Hentai([Remainder]string tag = null)
        {
            var retry = 5;
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorMessageAsync($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "%20");

            var image = await _service.GetImage(tag);

            while (retry > 0)
            {
                if (String.IsNullOrEmpty(image))
                {
                    image = await _service.GetImage(tag);
                    retry = 0;
                }
                else
                {
                    retry--;
                }
            }

            try
            {
                if (!String.IsNullOrEmpty(image))
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithImageUrl(image);

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
                else
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                    embed.WithDescription("I couldn't find anything.");

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }
            catch
            {

            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Danbooru([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorMessageAsync($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "%20");

            var image = await _service.DownloadImages(NSFWService.NSFWSite.Danbooru, tag);

            try
            {
                if (!String.IsNullOrEmpty(image))
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithImageUrl(image);

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
                else
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                    embed.WithDescription("I couldn't find anything.");

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }
            catch
            {

            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Konachan([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorMessageAsync($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "%20");

            var image = await _service.DownloadImages(NSFWService.NSFWSite.Konachan, tag);

            try
            {
                if (!String.IsNullOrEmpty(image))
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithImageUrl(image);

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
                else
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                    embed.WithDescription("I couldn't find anything.");

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }
            catch
            {

            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(3, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Yandere([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorMessageAsync($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "%20");

            var image = await _service.DownloadImages(NSFWService.NSFWSite.Yandere, tag);

            try
            {
                if (!String.IsNullOrEmpty(image))
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithImageUrl(image);

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
                else
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                    embed.WithDescription("I couldn't find anything.");

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }
            catch
            {

            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Ratelimit(1, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task HentaiPlus([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorMessageAsync($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            tag = tag?.Replace(" ", "%20");

            string[] images =
            {
                await _service.DownloadImages(NSFWService.NSFWSite.Danbooru, tag),
                await _service.DownloadImages(NSFWService.NSFWSite.Konachan, tag),
                await _service.DownloadImages(NSFWService.NSFWSite.Yandere, tag)
            };

            try
            {
                var count = 0;
                foreach (var image in images)
                {
                    if (!String.IsNullOrEmpty(image))
                    {
                        await Context.Channel.SendMessageAsync(image);
                        count++;
                    }
                }
                if (count == 0)
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithColor(RiasBot.BadColor);
                    embed.WithDescription("I couldn't find anything.");

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }
            catch
            {

            }
        }
    }
}
