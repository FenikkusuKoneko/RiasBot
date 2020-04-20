using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Emojis")]
        public class EmojisSubmodule : RiasModule
        {
            private readonly HttpClient _httpClient;
            
            public EmojisSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }
            
            [Command("addemoji"), Context(ContextType.Guild),
             UserPermission(Permission.ManageEmojis), BotPermission(Permission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddEmojiAsync(string url, [Remainder] string name)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var emojiUri))
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotValid);
                    return;
                }

                if (emojiUri.Scheme != Uri.UriSchemeHttps)
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotHttps);
                    return;
                }

                using var result = await _httpClient.GetAsync(emojiUri);
                if (!result.IsSuccessStatusCode)
                {
                    await ReplyErrorAsync(Localization.UtilityImageOrUrlNotGood);
                    return;
                }

                await using var stream = await result.Content.ReadAsStreamAsync();
                await using var emojiStream = new MemoryStream();
                await stream.CopyToAsync(emojiStream);
                emojiStream.Position = 0;

                bool? isAnimated = null;
                if (RiasUtilities.IsPng(emojiStream) || RiasUtilities.IsJpg(emojiStream))
                    isAnimated = false;

                if (!isAnimated.HasValue && RiasUtilities.IsGif(emojiStream))
                    isAnimated = true;

                if (!isAnimated.HasValue)
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotPngJpgGif);
                    return;
                }

                var emojis = Context.Guild!.Emojis.Values;
                var emojisSlots = Context.Guild!.GetGuildEmotesSlots();
                if (isAnimated.Value)
                {
                    if (emojis.Count(x => x.IsAnimated) >= emojisSlots)
                    {
                        await ReplyErrorAsync(Localization.AdministrationAnimatedEmojisLimit, emojisSlots);
                        return;
                    }
                }
                else
                {
                    if (emojis.Count(x => !x.IsAnimated) >= emojisSlots)
                    {
                        await ReplyErrorAsync(Localization.AdministrationStaticEmojisLimit, emojisSlots);
                        return;
                    }
                }

                if (emojiStream.Length / 1024 > 256) //in KB
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiSizeLimit);
                    return;
                }

                name = name.Replace(" ", "_");
                emojiStream.Position = 0;
                await Context.Guild.CreateEmojiAsync(emojiStream, name);
                await ReplyConfirmationAsync(Localization.AdministrationEmojiCreated, name);
            }
            
            [Command("deleteemoji"), Context(ContextType.Guild),
             UserPermission(Permission.ManageEmojis), BotPermission(Permission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteEmojiAsync([Remainder] string name)
            {
                var emoji = await GetEmojiAsync(name);
                if (emoji is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotFound);
                    return;
                }

                try
                {
                    await Context.Guild!.DeleteEmojiAsync(emoji.Id);
                    await ReplyConfirmationAsync(Localization.AdministrationEmojiDeleted, emoji.Name);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotDeleted);
                }
            }
            
            [Command("renameemoji"), Context(ContextType.Guild),
             UserPermission(Permission.ManageEmojis), BotPermission(Permission.ManageEmojis),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameEmojiAsync([Remainder] string names)
            {
                var emojis = names.Split("->");

                if (emojis.Length < 2)
                    return;

                var oldName = emojis[0].TrimEnd();
                var newName = emojis[1].TrimStart();

                var emoji = await GetEmojiAsync(oldName);
                if (emoji is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotFound);
                    return;
                }

                try
                {
                    newName = newName.Replace(" ", "_");
                    await Context.Guild!.ModifyEmojiAsync(emoji.Id, x => x.Name = newName);
                    await ReplyConfirmationAsync(Localization.AdministrationEmojiRenamed, oldName, newName);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotRenamed);
                }
            }
            
            private async Task<ICustomEmoji> GetEmojiAsync(string value)
            {
                if (LocalCustomEmoji.TryParse(value, out var emoji))
                    return Context.Guild!.Emojis.FirstOrDefault(x => x.Value.Id == emoji.Id).Value
                           ?? (ICustomEmoji) await Context.Guild!.GetEmojiAsync(emoji.Id);

                if (ulong.TryParse(value, out var emojiId))
                    return Context.Guild!.Emojis.FirstOrDefault(x => x.Value.Id == emojiId).Value
                           ?? (ICustomEmoji) await Context.Guild!.GetEmojiAsync(emojiId);

                value = value.Replace(" ", "_");
                return Context.Guild!.Emojis.FirstOrDefault(e => string.Equals(e.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            }
        }
    }
}