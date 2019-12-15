using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Serilog;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Emotes")]
        public class Emotes : RiasModule
        {
            private readonly HttpClient _httpClient;

            public Emotes(IServiceProvider services) : base(services)
            {
                _httpClient = services.GetRequiredService<HttpClient>();
            }

            [Command("addemote"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageEmojis), BotPermission(GuildPermission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddEmoteAsync(string url, [Remainder] string name)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var emoteUri))
                {
                    await ReplyErrorAsync("#Utility_UrlNotValid");
                    return;
                }

                if (emoteUri.Scheme != Uri.UriSchemeHttps)
                {
                    await ReplyErrorAsync("#Utility_UrlNotHttps");
                    return;
                }

                using var result = await _httpClient.GetAsync(emoteUri);
                if (!result.IsSuccessStatusCode)
                {
                    await ReplyErrorAsync("#Utility_ImageOrUrlNotGood");
                    return;
                }

                await using var emoteStream = await result.Content.ReadAsStreamAsync();

                bool? isAnimated = null;
                if (RiasUtils.IsPng(emoteStream) || RiasUtils.IsJpg(emoteStream))
                    isAnimated = false;

                if (!isAnimated.HasValue && RiasUtils.IsGif(emoteStream))
                    isAnimated = true;

                Log.Debug($"IsAnimated: {isAnimated}");

                if (!isAnimated.HasValue)
                {
                    await ReplyErrorAsync("#Utility_UrlNotPngJpgGif");
                    return;
                }

                var emotes = Context.Guild!.Emotes;
                var emotesSlots = Context.Guild!.GetGuildEmotesSlots();
                if (isAnimated.Value)
                {
                    if (emotes.Count(x => x.Animated) >= emotesSlots)
                    {
                        await ReplyErrorAsync("AnimatedEmotesLimit", emotesSlots);
                        return;
                    }
                }
                else
                {
                    if (emotes.Count(x => !x.Animated) >= emotesSlots)
                    {
                        await ReplyErrorAsync("StaticEmotesLimit", emotesSlots);
                        return;
                    }
                }

                if (emoteStream.Length / 1024 > 256) //in KB
                {
                    await ReplyErrorAsync("EmoteSizeLimit");
                    return;
                }

                name = name.Replace(" ", "_");
                emoteStream.Position = 0;
                using var image = new Image(emoteStream);
                await Context.Guild.CreateEmoteAsync(name, image);
                await ReplyConfirmationAsync("EmoteCreated", name);
            }

            [Command("deleteemote"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageEmojis), BotPermission(GuildPermission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteEmoteAsync([Remainder] string name)
            {
                var emote = await GetEmoteAsync(name);
                if (emote is null)
                {
                    await ReplyErrorAsync("EmoteNotFound");
                    return;
                }

                try
                {
                    await Context.Guild!.DeleteEmoteAsync(emote);
                    await ReplyConfirmationAsync("EmoteDeleted", emote.Name);
                }
                catch
                {
                    await ReplyErrorAsync("EmoteNotDeleted");
                }
            }

            [Command("renameemote"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageEmojis), BotPermission(GuildPermission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameEmoteAsync([Remainder] string names)
            {
                var emotes = names.Split("->");

                if (emotes.Length < 2)
                    return;

                var oldName = emotes[0].TrimEnd();
                var newName = emotes[1].TrimStart();

                var emote = await GetEmoteAsync(oldName);
                if (emote is null)
                {
                    await ReplyErrorAsync("EmoteNotFound");
                    return;
                }

                try
                {
                    newName = newName.Replace(" ", "_");
                    await Context.Guild!.ModifyEmoteAsync(emote, x => x.Name = newName);
                    await ReplyConfirmationAsync("EmoteRenamed", oldName, newName);
                }
                catch
                {
                    await ReplyErrorAsync("EmoteNotRenamed");
                }
            }

            private async Task<GuildEmote> GetEmoteAsync(string value)
            {
                if (Emote.TryParse(value, out var emote))
                    return await Context.Guild!.GetEmoteAsync(emote.Id);

                if (ulong.TryParse(value, out var emoteId))
                    return await Context.Guild!.GetEmoteAsync(emoteId);

                value = value.Replace(" ", "_");
                return Context.Guild!.Emotes.FirstOrDefault(e => string.Equals(e.Name, value, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}