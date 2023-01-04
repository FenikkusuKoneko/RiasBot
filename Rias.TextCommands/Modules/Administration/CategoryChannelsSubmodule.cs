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
        public async Task<IResult> CreateCategoryAsync([Remainder] string name)
        {
            if (name.Length is < 1 or > 100)
                return ReplyErrorResponse(Strings.Administration.ChannelNameLengthLimit, 1, 100);

            await Context.GetGuild().CreateCategoryChannelAsync(name);
            return ReplySuccessResponse(Strings.Administration.CategoryChannelCreated, name);
        }
        
        [TextCommand("renamecategory", "rcat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> RenameCategoryAsync(ICategoryChannel category, [Remainder] string name)
        {
            if (name.Length is < 1 or > 100)
                return ReplyErrorResponse(Strings.Administration.ChannelNameLengthLimit, 1, 100);
            
            if ((Context.Author.CalculateChannelPermissions(category) & Permissions.ManageChannels) == 0)
                return ReplyErrorResponse(Strings.Administration.AuthorMissingChannelManagePermission, category.Mention);
            
            if (Context.GetCurrentMember().CalculateChannelPermissions(category) == 0)
                return ReplyErrorResponse(Strings.Administration.BotMissingChannelManagePermission, category.Mention);

            await category.ModifyAsync(props => props.Name = name);
            return ReplySuccessResponse(Strings.Administration.CategoryChannelRenamed, category.Name, name);
        }
        
        [TextCommand("deletecategory", "dcat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
        public async Task<IResult> DeleteCategoryAsync([Remainder] ICategoryChannel category)
        {
            if ((Context.Author.CalculateChannelPermissions(category) & Permissions.ManageChannels) == 0)
                return ReplyErrorResponse(Strings.Administration.AuthorMissingChannelManagePermission, category.Mention);
            
            if (Context.GetCurrentMember().CalculateChannelPermissions(category) == 0)
                return ReplyErrorResponse(Strings.Administration.BotMissingChannelManagePermission, category.Mention);

            await category.DeleteAsync();
            return ReplySuccessResponse(Strings.Administration.CategoryChannelDeleted, category.Name);
        }

        [TextCommand("addtextchanneltocategory", "atchtocat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        public async Task<IResult> AddTextChannelToCategoryAsync(ITextChannel channel, [Remainder] ICategoryChannel category)
        {
            if ((Context.Author.CalculateChannelPermissions(channel) & Permissions.ManageChannels) == 0)
                return ReplyErrorResponse(Strings.Administration.AuthorMissingChannelManagePermission, channel.Mention);
            
            if (Context.GetCurrentMember().CalculateChannelPermissions(channel) == 0)
                return ReplyErrorResponse(Strings.Administration.BotMissingChannelManagePermission, channel.Mention);

            await channel.ModifyAsync(props => props.CategoryId = category.Id);
            return ReplySuccessResponse(Strings.Administration.TextChannelAddedToCategory, channel.Mention, category.Name);
        }
        
        [TextCommand("addvoicechanneltocategory", "avchtocat")]
        [AuthorPermissions(Permissions.ManageChannels)]
        [BotPermissions(Permissions.ManageChannels)]
        public async Task<IResult> AddVoiceChannelToCategoryAsync(IVoiceChannel channel, [Remainder] ICategoryChannel category)
        {
            if ((Context.Author.CalculateChannelPermissions(channel) & Permissions.ManageChannels) == 0)
                return ReplyErrorResponse(Strings.Administration.AuthorMissingChannelManagePermission, channel.Mention);
            
            if (Context.GetCurrentMember().CalculateChannelPermissions(channel) == 0)
                return ReplyErrorResponse(Strings.Administration.BotMissingChannelManagePermission, channel.Mention);

            await channel.ModifyAsync(props => props.CategoryId = category.Id);
            return ReplySuccessResponse(Strings.Administration.VoiceChannelAddedToCategory, channel.Mention, category.Name);
        }
    }
}