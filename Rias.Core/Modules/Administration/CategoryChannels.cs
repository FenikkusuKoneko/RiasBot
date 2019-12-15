using System;
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
            public CategoryChannels(IServiceProvider services) : base(services) {}

            [Command("createcategory"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateCategoryAsync([Remainder] string name)
            {
                if (name.Length < 1 || name.Length > 100)
                {
                    await ReplyErrorAsync("ChannelNameLengthLimit", 1, 100);
                    return;
                }

                await Context.Guild!.CreateCategoryChannelAsync(name);
                await ReplyConfirmationAsync("CategoryChannelCreated", name);
            }

            [Command("deletecategory"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteCategoryAsync([Remainder] SocketCategoryChannel category)
            {
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, category))
                {
                    await ReplyErrorAsync("CategoryChannelNoPermissionView");
                    return;
                }

                await category.DeleteAsync();
                await ReplyConfirmationAsync("CategoryChannelDeleted", category.Name);
            }

            [Command("renamecategory"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameCategoryAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");
                if (namesSplit.Length < 2)
                    return;

                var oldName = namesSplit[0].TrimEnd();
                var newName = namesSplit[1].TrimStart();

                var category = Context.Guild!.GetCategoryChannel(oldName);

                if (category is null)
                {
                    await ReplyErrorAsync("CategoryChannelNotFound");
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, category))
                {
                    await ReplyErrorAsync("CategoryChannelNoViewPermission");
                    return;
                }

                if (newName.Length < 1 || newName.Length > 100)
                {
                    await ReplyErrorAsync("ChannelNameLengthLimit", 1, 100);
                    return;
                }

                oldName = category.Name;
                await category.ModifyAsync(x => x.Name = newName);
                await ReplyConfirmationAsync("CategoryChannelRenamed", oldName, newName);
            }

            [Command("addtextchanneltocategory"),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddTextChannelToCategoryAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");

                if (namesSplit.Length < 2)
                    return;

                var channelName = namesSplit[0].TrimEnd();
                var categoryName = namesSplit[1].TrimStart();

                var channel = Context.Guild!.GetTextChannel(channelName);

                if (channel is null)
                {
                    await ReplyErrorAsync("TextChannelNotFound");
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, channel))
                {
                    await ReplyErrorAsync("TextChannelNoViewPermission");
                    return;
                }

                var category = Context.Guild!.GetCategoryChannel(categoryName);

                if (category is null)
                {
                    await ReplyErrorAsync("CategoryChannelNotFound");
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, category))
                {
                    await ReplyErrorAsync("CategoryChannelNoViewPermission");
                    return;
                }

                await channel.ModifyAsync(x => x.CategoryId = category.Id);
                await ReplyConfirmationAsync("TextChannelAddedToCategory", channel.Name, category.Name);
            }

            [Command("addvoicechanneltocategory"),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task AddVoiceChannelToCategoryAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");

                if (namesSplit.Length < 2)
                    return;

                var channelName = namesSplit[0].TrimEnd();
                var categoryName = namesSplit[1].TrimStart();

                var channel = Context.Guild!.GetVoiceChannel(channelName);

                if (channel is null)
                {
                    await ReplyErrorAsync("VoiceChannelNotFound");
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, channel))
                {
                    await ReplyErrorAsync("VoiceChannelNoViewPermission");
                    return;
                }

                var category = Context.Guild!.GetCategoryChannel(categoryName);

                if (category is null)
                {
                    await ReplyErrorAsync("CategoryChannelNotFound");
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, category))
                {
                    await ReplyErrorAsync("CategoryChannelNoViewPermission");
                    return;
                }

                await channel.ModifyAsync(x => x.CategoryId = category.Id);
                await ReplyConfirmationAsync("VoiceChannelAddedToCategory", channel.Name, category.Name);
            }
        }
    }
}