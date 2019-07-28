using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Category Channels")]
        public class CategoryChannels : RiasModule
        {
            [Command("createcategory"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateCategoryAsync([Remainder] string name)
            {
                if (name.Length < 1 || name.Length > 100)
                {
                    await ReplyErrorAsync("channel_name_length_limit");
                    return;
                }
                
                await Context.Guild.CreateCategoryChannelAsync(name);
                await ReplyConfirmationAsync("category_created", name);
            }
            
            [Command("deletecategory"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteCategoryAsync([Remainder] SocketCategoryChannel category)
            {
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser, category))
                {
                    await ReplyErrorAsync("category_no_permission_view");
                    return;
                }

                await category.DeleteAsync();
                await ReplyConfirmationAsync("category_deleted", category.Name);
            }

            [Command("renamecategory"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameCategoryAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");
                if (namesSplit.Length < 2)
                    return;
                
                var oldName = namesSplit[0].TrimEnd();
                var newName = namesSplit[1].TrimStart();
                
                var category = await ChannelExtensions.GetCategoryByIdAsync(Context.Guild, oldName)
                               ?? Context.Guild.CategoryChannels.FirstOrDefault(x => string.Equals(x.Name, oldName, StringComparison.OrdinalIgnoreCase));

                if (category is null)
                {
                    await ReplyErrorAsync("category_not_found");
                    return;
                }
                
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser, category))
                {
                    await ReplyErrorAsync("category_no_permission_view");
                    return;
                }
                
                oldName = category.Name;
                await category.ModifyAsync(x => x.Name = newName);
                await ReplyConfirmationAsync("category_renamed", oldName, newName);
            }

            [Command("addtextchanneltocategory"),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddTextChannelToCategoryAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");

                if (namesSplit.Length < 2)
                    return;
                
                var channelName = namesSplit[0].TrimEnd();
                var categoryName = namesSplit[1].TrimStart();
                
                var channel = await ChannelExtensions.GetTextChannelByMentionOrIdAsync(Context.Guild, channelName)
                              ?? Context.Guild.TextChannels.FirstOrDefault(x => x.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));

                if (channel is null)
                {
                    await ReplyErrorAsync("text_channel_not_found");
                    return;
                }
                
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser, channel))
                {
                    await ReplyErrorAsync("text_channel_no_permission_view");
                    return;
                }
                
                var category = await ChannelExtensions.GetCategoryByIdAsync(Context.Guild, categoryName)
                               ?? Context.Guild.CategoryChannels.FirstOrDefault(x => x.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                if (category is null)
                {
                    await ReplyErrorAsync("category_not_found");
                    return;
                }
                
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser, category))
                {
                    await ReplyErrorAsync("category_no_permission_view");
                    return;
                }

                await channel.ModifyAsync(x => x.CategoryId = category.Id);
                await ReplyConfirmationAsync("text_channel_added_to_category", channel.Name, category.Name);
            }
            
            [Command("addvoicechanneltocategory"),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddVoiceChannelToCategoryAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");

                if (namesSplit.Length < 2)
                    return;
                
                var channelName = namesSplit[0].TrimEnd();
                var categoryName = namesSplit[1].TrimStart();
                
                var channel = await ChannelExtensions.GetVoiceChannelByIdAsync(Context.Guild, channelName)
                              ?? Context.Guild.VoiceChannels.FirstOrDefault(x => x.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));

                if (channel is null)
                {
                    await ReplyErrorAsync("voice_channel_not_found");
                    return;
                }
                
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser, channel))
                {
                    await ReplyErrorAsync("voice_channel_no_permission_view");
                    return;
                }
                
                var category = await ChannelExtensions.GetCategoryByIdAsync(Context.Guild, categoryName)
                               ?? Context.Guild.CategoryChannels.FirstOrDefault(x => x.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                if (category is null)
                {
                    await ReplyErrorAsync("category_not_found");
                    return;
                }
                
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser, category))
                {
                    await ReplyErrorAsync("category_no_permission_view");
                    return;
                }

                await channel.ModifyAsync(x => x.CategoryId = category.Id);
                await ReplyConfirmationAsync("voice_channel_added_to_category", channel.Name, category.Name);
            }
        }
    }
}