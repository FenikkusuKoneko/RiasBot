using Disqord;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Services.Attributes;
using Rias.Services.Extensions;

namespace Rias.TextCommands.Modules.Administration;

public partial class AdministrationModule
{
    [Name("Category Channels")]
    public class CategoryChannelsSubmodule : RiasTextGuildModule
    {
        [TextCommand("createcategory", "ccat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> CreateCategory([Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Channel.MinNameLength or > Limits.Guild.Channel.MaxNameLength)
                return ErrorReply(Strings.Administration.ChannelNameLengthLimit, Limits.Guild.Channel.MinNameLength, Limits.Guild.Channel.MaxNameLength);

            await Context.GetGuild().CreateCategoryChannelAsync(name);
            return SuccessReply(Strings.Administration.CategoryChannelCreated, name);
        }

        [TextCommand("renamecategory", "rcat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> RenameCategory(ICategoryChannel category, [Remainder] string name)
        {
            if (name.Length is < Limits.Guild.Channel.MinNameLength or > Limits.Guild.Channel.MaxNameLength)
                return ErrorReply(Strings.Administration.ChannelNameLengthLimit, Limits.Guild.Channel.MinNameLength, Limits.Guild.Channel.MaxNameLength);

            if ((Context.Author.CalculateChannelPermissions(category) & Permissions.ManageChannels) == 0)
                return ErrorReply(Strings.Administration.AuthorMissingChannelManagePermission, category.Mention);

            if (Context.GetCurrentMember().CalculateChannelPermissions(category) == 0)
                return ErrorReply(Strings.Administration.BotMissingChannelManagePermission, category.Mention);

            await category.ModifyAsync(props => props.Name = name);
            return SuccessReply(Strings.Administration.CategoryChannelRenamed, category.Name, name);
        }

        [TextCommand("deletecategory", "dcat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> DeleteCategory([Remainder] ICategoryChannel category)
        {
            if ((Context.Author.CalculateChannelPermissions(category) & Permissions.ManageChannels) == 0)
                return ErrorReply(Strings.Administration.AuthorMissingChannelManagePermission, category.Mention);

            if (Context.GetCurrentMember().CalculateChannelPermissions(category) == 0)
                return ErrorReply(Strings.Administration.BotMissingChannelManagePermission, category.Mention);

            await category.DeleteAsync();
            return SuccessReply(Strings.Administration.CategoryChannelDeleted, category.Name);
        }

        [TextCommand("addtextchanneltocategory", "atchtocat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        public async Task<IResult> AddTextChannelToCategory(ITextChannel channel, [Remainder] ICategoryChannel category)
        {
            if ((Context.Author.CalculateChannelPermissions(channel) & Permissions.ManageChannels) == 0)
                return ErrorReply(Strings.Administration.AuthorMissingChannelManagePermission, channel.Mention);

            if (Context.GetCurrentMember().CalculateChannelPermissions(channel) == 0)
                return ErrorReply(Strings.Administration.BotMissingChannelManagePermission, channel.Mention);

            await channel.ModifyAsync(props => props.CategoryId = category.Id);
            return SuccessReply(Strings.Administration.TextChannelAddedToCategory, channel.Mention, category.Name);
        }

        [TextCommand("addvoicechanneltocategory", "avchtocat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        public async Task<IResult> AddVoiceChannelToCategory(IVoiceChannel channel, [Remainder] ICategoryChannel category)
        {
            if ((Context.Author.CalculateChannelPermissions(channel) & Permissions.ManageChannels) == 0)
                return ErrorReply(Strings.Administration.AuthorMissingChannelManagePermission, channel.Mention);

            if (Context.GetCurrentMember().CalculateChannelPermissions(channel) == 0)
                return ErrorReply(Strings.Administration.BotMissingChannelManagePermission, channel.Mention);

            await channel.ModifyAsync(props => props.CategoryId = category.Id);
            return SuccessReply(Strings.Administration.VoiceChannelAddedToCategory, channel.Mention, category.Name);
        }
    }
}