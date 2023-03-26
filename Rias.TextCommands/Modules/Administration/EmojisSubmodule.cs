using Disqord;
using Disqord.Bot.Commands;
using Disqord.Rest;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Services.Attributes;
using Rias.Services.Commands;
using Rias.Services.Extensions;

namespace Rias.TextCommands.Modules.Administration;

public partial class AdministrationModule
{
    [Name("Emojis")]
    public class EmojisSubmodule : RiasTextGuildModule<EmojisService>
    {
        [TextCommand("addemoji", "ae")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> AddEmoji(ICustomEmoji emoji, [Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Emoji.MinNameLength or > Limits.Guild.Emoji.MaxNameLength)
                return ErrorReply(Strings.Administration.EmojiNameLengthLimit, Limits.Guild.Emoji.MinNameLength, Limits.Guild.Emoji.MaxNameLength);

            var result = await Service.AddEmojiAsync(emoji, Context.GetGuild(), name);

            return result.IsSuccessful
                ? SuccessReply(Strings.Administration.EmojiCreated, result.Value.Name)
                : ErrorReply(result);
        }

        [TextCommand("addemoji", "ae")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> AddEmoji(string url, [Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Emoji.MinNameLength or > Limits.Guild.Emoji.MaxNameLength)
                return ErrorReply(Strings.Administration.EmojiNameLengthLimit, Limits.Guild.Emoji.MinNameLength, Limits.Guild.Emoji.MaxNameLength);

            var result = await Service.AddEmojiAsync(url, Context.GetGuild(), name);

            return result.IsSuccessful
                ? SuccessReply(Strings.Administration.EmojiCreated, result.Value.Name)
                : ErrorReply(result);
        }

        [TextCommand("addemoji", "ae")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> AddEmoji([Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Emoji.MinNameLength or > Limits.Guild.Emoji.MaxNameLength)
                return ErrorReply(Strings.Administration.EmojiNameLengthLimit, Limits.Guild.Emoji.MinNameLength, Limits.Guild.Emoji.MaxNameLength);

            if (Context.Message.Attachments.Count == 0)
                return ErrorReply(Strings.Utility.NoImageAttached);

            var result = await Service.AddEmojiAsync(Context.Message.Attachments[0].Url, Context.GetGuild(), name);

            return result.IsSuccessful
                ? SuccessReply(Strings.Administration.EmojiCreated, result.Value.Name)
                : ErrorReply(result);
        }

        [TextCommand("deleteemoji", "delemoji", "de")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> DeleteEmoji(IGuildEmoji emoji)
        {
            await Context.GetGuild().DeleteEmojiAsync(emoji.Id);
            return SuccessReply(Strings.Administration.EmojiDeleted, emoji.Name);
        }

        [TextCommand("renameemoji", "rnemoji", "re")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> RenameEmoji(IGuildEmoji emoji, [Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Emoji.MinNameLength or > Limits.Guild.Emoji.MaxNameLength)
                return ErrorReply(Strings.Administration.EmojiNameLengthLimit, Limits.Guild.Emoji.MinNameLength, Limits.Guild.Emoji.MaxNameLength);

            name = name.Replace(" ", "");
            await Context.GetGuild().ModifyEmojiAsync(emoji.Id, props => props.Name = name);
            return SuccessReply(Strings.Administration.EmojiRenamed, emoji.Name, name);
        }

        [TextCommand("emoji")]
        public IResult Emoji(ICustomEmoji emoji)
        {
            var embed = SuccessEmbed
                .WithTitle(emoji.Name ?? emoji.Id.ToString())
                .WithImageUrl(emoji.GetUrl(CdnAssetFormat.Automatic, 128));

            return Reply(embed);
        }
    }
}