using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Nsfw
{
    [Name("Nsfw")]
    public class Nsfw : RiasModule<NsfwService>
    {
        public Nsfw(IServiceProvider services) : base(services)
        {
        }

        [Command("hentai"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Channel)]
        public async Task HentaiAsync([Remainder] string? tags = null)
            => await PostHentaiAsync(NsfwService.NsfwImageApiProvider.Random, tags);

        [Command("danbooru"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Channel)]
        public async Task DanbooruAsync([Remainder] string? tags = null)
            => await PostHentaiAsync(NsfwService.NsfwImageApiProvider.Danbooru, tags);
        
        [Command("konachan"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Channel)]
        public async Task KonachanAsync([Remainder] string? tags = null)
            => await PostHentaiAsync(NsfwService.NsfwImageApiProvider.Konachan, tags);
        
        [Command("yandere"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Channel)]
        public async Task YandereAsync([Remainder] string? tags = null)
            => await PostHentaiAsync(NsfwService.NsfwImageApiProvider.Yandere, tags);
        
        [Command("gelbooru"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Channel)]
        public async Task GelbooruAsync([Remainder] string? tags = null)
            => await PostHentaiAsync(NsfwService.NsfwImageApiProvider.Gelbooru, tags);

        [Command("hentaiplus"), Context(ContextType.Guild),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Channel)]
        public async Task HentaiPlusAsync([Remainder] string? tags = null)
        {
            if (!((SocketTextChannel) Context.Channel).IsNsfw)
            {
                await ReplyErrorAsync("ChannelNotNsfw");
                return;
            }
            
            if (!Service.CacheInitialized)
            {
                await ReplyErrorAsync("CacheNotInitialized");
                return;
            }
            
            var hentaiBuilder = new StringBuilder();
            var danbooruHentai = await Service.GetNsfwImageAsync(NsfwService.NsfwImageApiProvider.Danbooru, tags);
            var konachanHentai = await Service.GetNsfwImageAsync(NsfwService.NsfwImageApiProvider.Konachan, tags);
            var yandereHentai = await Service.GetNsfwImageAsync(NsfwService.NsfwImageApiProvider.Yandere, tags);
            var gelbooruHentai = await Service.GetNsfwImageAsync(NsfwService.NsfwImageApiProvider.Gelbooru, tags);

            if (danbooruHentai != null)
                hentaiBuilder.Append(danbooruHentai.Url).Append("\n");
            if (konachanHentai != null)
                hentaiBuilder.Append(konachanHentai.Url).Append("\n");
            if (yandereHentai != null)
                hentaiBuilder.Append(yandereHentai.Url).Append("\n");
            if (gelbooruHentai != null)
                hentaiBuilder.Append(gelbooruHentai.Url).Append("\n");

            if (hentaiBuilder.Length == 0)
            {
                await ReplyErrorAsync("NoHentai");
                return;
            }

            await Context.Channel.SendMessageAsync(hentaiBuilder.ToString());
        }

        private async Task PostHentaiAsync(NsfwService.NsfwImageApiProvider provider, string? tags = null)
        {
            if (!((SocketTextChannel) Context.Channel).IsNsfw)
            {
                await ReplyErrorAsync("ChannelNotNsfw");
                return;
            }
            
            if (!Service.CacheInitialized)
            {
                await ReplyErrorAsync("CacheNotInitialized");
                return;
            }
            
            var nsfwImage = await Service.GetNsfwImageAsync(provider, tags);
            if (nsfwImage is null)
            {
                await ReplyErrorAsync("NoHentai");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Author = new EmbedAuthorBuilder
                {
                    Name = $"{GetText("#Searches_Source")}: {nsfwImage.Provider}",
                    Url = nsfwImage.Url
                },
                ImageUrl = nsfwImage.Url
            };

            await ReplyAsync(embed);
        }
    }
}