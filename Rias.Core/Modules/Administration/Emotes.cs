using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Emotes")]
        public class Emotes : RiasModule
        {
            [Command("addemote"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageEmojis), BotPermission(GuildPermission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddEmoteAsync(string url, [Remainder] string name)
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await ReplyErrorAsync("#utility_url_not_valid");
                    return;
                }

                if (!url.Contains("https"))
                {
                    await ReplyErrorAsync("#utility_url_not_https");
                    return;
                }

                var isAnimated = false;
                if (!(url.Contains(".png") || url.Contains(".jpg") || url.Contains(".jpeg")))
                {
                    if (!url.Contains(".gif"))
                    {
                        await ReplyErrorAsync("#utility_url_not_png_jpg_gif");
                        return;
                    }

                    isAnimated = true;
                }

                var emotes = Context.Guild.Emotes;
                var emotesSlots = Context.Guild.GetGuildEmotesSlots();
                if (isAnimated)
                {
                    if (emotes.Count(x => x.Animated) >= emotesSlots)
                    {
                        await ReplyErrorAsync("animated_emotes_limit", emotesSlots);
                        return;
                    }
                }
                else
                {
                    if (emotes.Count(x => !x.Animated) >= emotesSlots)
                    {
                        await ReplyErrorAsync("static_emotes_limit", emotesSlots);
                        return;
                    }
                }

                using var http = new HttpClient();
                try
                {
                    using var res = await http.GetAsync(new Uri(url));
                    if (!res.IsSuccessStatusCode)
                    {
                        await ReplyErrorAsync("#utility_image_or_url_not_good");
                        return;
                    }

                    await using var emoteStream = await res.Content.ReadAsStreamAsync();
                    if (emoteStream.Length / 1024 > 256) //in KB
                    {
                        await ReplyErrorAsync("emote_size_limit");
                        return;
                    }

                    name = name.Replace(" ", "_");
                    await Context.Guild.CreateEmoteAsync(name, new Image(emoteStream));
                    await ReplyConfirmationAsync("emote_created", name);
                }
                catch
                {
                    await ReplyErrorAsync("#utility_image_or_url_not_good");
                }
            }

            [Command("deleteemote"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageEmojis), BotPermission(GuildPermission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteEmoteAsync([Remainder] string name)
            {
                var emote = await GetEmoteAsync(name);
                if (emote is null)
                {
                    await ReplyErrorAsync("emote_not_found");
                    return;
                }

                try
                {
                    await Context.Guild.DeleteEmoteAsync(emote);
                    await ReplyConfirmationAsync("emote_deleted", emote.Name);
                }
                catch
                {
                    await ReplyErrorAsync("emote_not_deleted");
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
                    await ReplyErrorAsync("emote_not_found");
                    return;
                }

                try
                {
                    newName = newName.Replace(" ", "_");
                    await Context.Guild.ModifyEmoteAsync(emote, x => x.Name = newName);
                    await ReplyConfirmationAsync("emote_renamed", oldName, newName);
                }
                catch
                {
                    await ReplyErrorAsync("emote_not_renamed");
                }
            }

            private async Task<GuildEmote> GetEmoteAsync(string value)
            {
                if (Emote.TryParse(value, out var emote))
                    return await Context.Guild.GetEmoteAsync(emote.Id);

                if (ulong.TryParse(value, out var emoteId))
                    return await Context.Guild.GetEmoteAsync(emoteId);

                value = value.Replace(" ", "_");
                return Context.Guild.Emotes.FirstOrDefault(e => string.Equals(e.Name, value, StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }
}