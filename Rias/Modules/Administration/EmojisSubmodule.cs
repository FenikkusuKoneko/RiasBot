using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Emojis")]
        public class EmojisSubmodule : RiasModule
        {
            private readonly HttpClient _httpClient;
            
            public EmojisSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }
            
            [Command("addemoji")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageEmojis)]
            [BotPermission(Permissions.ManageEmojis)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
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
                
                // Check if length is bigger than 256 KB
                if (emojiStream.Length / 1024 > 256)
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiSizeLimit);
                    return;
                }

                name = name.Replace(" ", "_");
                emojiStream.Position = 0;
                await Context.Guild.CreateEmojiAsync(name, emojiStream);
                await ReplyConfirmationAsync(Localization.AdministrationEmojiCreated, name);
            }
            
            [Command("deleteemoji")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageEmojis)]
            [BotPermission(Permissions.ManageEmojis)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteEmojiAsync([Remainder] string name)
            {
                var emoji = GetEmoji(name);
                if (emoji is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotFound);
                    return;
                }

                try
                {
                    var guildEmoji = await Context.Guild!.GetEmojiAsync(emoji.Id);
                    await Context.Guild.DeleteEmojiAsync(guildEmoji);
                    await ReplyConfirmationAsync(Localization.AdministrationEmojiDeleted, emoji.Name);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotDeleted);
                }
            }
            
            [Command("renameemoji")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageEmojis)]
            [BotPermission(Permissions.ManageEmojis)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameEmojiAsync([Remainder] string names)
            {
                var emojis = names.Split("->");

                if (emojis.Length < 2)
                    return;

                var oldName = emojis[0].TrimEnd();
                var newName = emojis[1].TrimStart();

                var emoji = GetEmoji(oldName);
                if (emoji is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotFound);
                    return;
                }

                try
                {
                    newName = newName.Replace(" ", "_");
                    var guildEmoji = await Context.Guild!.GetEmojiAsync(emoji.Id);
                    await Context.Guild.ModifyEmojiAsync(guildEmoji, newName);
                    await ReplyConfirmationAsync(Localization.AdministrationEmojiRenamed, oldName, newName);
                }
                catch
                {
                    await ReplyErrorAsync(Localization.AdministrationEmojiNotRenamed);
                }
            }
            
            private DiscordEmoji? GetEmoji(string value)
            {
                if (!RiasUtilities.TryParseEmoji(value, out var emojiId))
                    ulong.TryParse(value, out emojiId);
                
                if (emojiId > 0)
                {
                    var emoji = Context.Guild!.Emojis.FirstOrDefault(x => x.Value.Id == emojiId).Value;
                    if (emoji is not null)
                        return emoji;
                }

                value = value.Replace(" ", "_");
                return Context.Guild!.Emojis.FirstOrDefault(e => string.Equals(e.Value.Name, value, StringComparison.OrdinalIgnoreCase)).Value;
            }
        }
    }
}