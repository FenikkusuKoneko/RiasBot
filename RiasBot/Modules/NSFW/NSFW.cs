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
    public partial class NSFW : RiasModule<NSFWService>
    {
        public NSFW()
        {

        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Hentai([Remainder]string tag = null)
        {
            int retry = 5;
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorEmbed($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "_");

            string image = await _service.GetImage(tag);

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

            if (!String.IsNullOrEmpty(image))
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithImageUrl(image);

                await ReplyAsync("", embed: embed.Build());
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
                embed.WithDescription("I couldn't find anything.");

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Danbooru([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorEmbed($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "_");

            string image = await _service.DownloadImages(NSFWService.NSFWSite.Danbooru, tag);

            if (!String.IsNullOrEmpty(image))
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithImageUrl(image);

                await ReplyAsync("", embed: embed.Build());
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
                embed.WithDescription("I couldn't find anything.");

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Konachan([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorEmbed($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "_");

            string image = await _service.DownloadImages(NSFWService.NSFWSite.Konachan, tag);

            if (!String.IsNullOrEmpty(image))
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithImageUrl(image);

                await ReplyAsync("", embed: embed.Build());
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
                embed.WithDescription("I couldn't find anything.");

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Yandere([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorEmbed($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            tag = tag?.Replace(" ", "_");

            string image = await _service.DownloadImages(NSFWService.NSFWSite.Yandere, tag);

            if (!String.IsNullOrEmpty(image))
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithImageUrl(image);

                await ReplyAsync("", embed: embed.Build());
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
                embed.WithDescription("I couldn't find anything.");

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task HentaiPlus([Remainder]string tag = null)
        {
            var channel = (ITextChannel)Context.Channel;
            if (!channel.IsNsfw)
            {
                await channel.SendErrorEmbed($"{Context.User.Mention} you can't use nsfw commands in a non-nsfw channel");
                return;
            }
            await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
            tag = tag?.Replace(" ", "_");

            string[] images =
            {
                await _service.DownloadImages(NSFWService.NSFWSite.Danbooru, tag),
                await _service.DownloadImages(NSFWService.NSFWSite.Konachan, tag),
                await _service.DownloadImages(NSFWService.NSFWSite.Yandere, tag)
            };

            int count = 0;
            foreach (var image in images)
            {
                if (!String.IsNullOrEmpty(image))
                {
                    Console.WriteLine(image);
                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                    embed.WithImageUrl(image);
                    await ReplyAsync("", embed: embed.Build());
                    count++;
                }
            }
            if (count == 0)
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithColor(RiasBot.badColor);
                embed.WithDescription("I couldn't find anything.");

                await ReplyAsync("", embed: embed.Build());
            }
        }
    }
}
