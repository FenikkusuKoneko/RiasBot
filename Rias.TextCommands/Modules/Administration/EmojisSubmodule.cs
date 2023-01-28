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
        [TextCommand("addemoji", "addemote", "ae")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> AddEmojiAsync(ICustomEmoji emoji, [Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Emoji.MinNameLength or > Limits.Guild.Emoji.MaxNameLength)
                return ErrorReply(Strings.Administration.EmojiNameLengthLimit, Limits.Guild.Emoji.MinNameLength, Limits.Guild.Emoji.MaxNameLength);
            
            var result = await Service.AddEmojiAsync(emoji, Context.GetGuild(), name);
            
            return result.IsSuccessful
                ? SuccessReply(Strings.Administration.EmojiCreated, result.Value.Name)
                : ErrorReply(result);
        }
        
        [TextCommand("addemoji", "addemote", "ae")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> AddEmojiAsync(string url, [Remainder] string name)
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
        public async Task<IResult> AddEmojiAsync([Remainder] string name)
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
        public async Task<IResult> DeleteEmojiAsync(IGuildEmoji emoji)
        {
            await Context.GetGuild().DeleteEmojiAsync(emoji.Id);
            return SuccessReply(Strings.Administration.EmojiDeleted, emoji.Name);
        }
        
        [TextCommand("renameemoji", "rnemoji", "re")]
        [AuthorPermissions(Permissions.ManageEmojisAndStickers)]
        [BotPermissions(Permissions.ManageEmojisAndStickers)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> RenameEmojiAsync(IGuildEmoji emoji, [Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Emoji.MinNameLength or > Limits.Guild.Emoji.MaxNameLength)
                return ErrorReply(Strings.Administration.EmojiNameLengthLimit, Limits.Guild.Emoji.MinNameLength, Limits.Guild.Emoji.MaxNameLength);
            
            name = name.Replace(" ", "");
            await Context.GetGuild().ModifyEmojiAsync(emoji.Id, props => props.Name = name);
            return SuccessReply(Strings.Administration.EmojiRenamed, emoji.Name, name);
        }

        [TextCommand("emoji")]
        public IResult EmojiAsync(ICustomEmoji emoji)
        {
            var embed = new LocalEmbed()
                .WithColor(Utils.SuccessColor)
                .WithTitle(emoji.Name ?? emoji.Id.ToString())
                .WithImageUrl(emoji.GetUrl(CdnAssetFormat.Automatic, 128));

            return Reply(embed);
        }
    }
}