using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Category Channels")]
        public class CategoryChannelsSubmodule : RiasModule
        {
            public CategoryChannelsSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
            
            [Command("createcategory", "ccat")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateCategoryAsync([Remainder] string name)
            {
                if (name.Length < 1 || name.Length > 100)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNameLengthLimit, 1, 100);
                    return;
                }

                await Context.Guild!.CreateChannelAsync(name, ChannelType.Category);
                await ReplyConfirmationAsync(Localization.AdministrationCategoryChannelCreated, name);
            }
            
            [Command("deletecategory", "dcat")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteCategoryAsync([CategoryChannel, Remainder] DiscordChannel category)
            {
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, category))
                {
                    await ReplyErrorAsync(Localization.AdministrationCategoryChannelNoViewPermission);
                    return;
                }

                await category.DeleteAsync();
                await ReplyConfirmationAsync(Localization.AdministrationCategoryChannelDeleted, category.Name);
            }

            [Command("renamecategory", "rncat")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
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
                    await ReplyErrorAsync(Localization.AdministrationCategoryChannelNotFound);
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, category))
                {
                    await ReplyErrorAsync(Localization.AdministrationCategoryChannelNoViewPermission);
                    return;
                }

                if (newName.Length < 1 || newName.Length > 100)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNameLengthLimit, 1, 100);
                    return;
                }

                oldName = category.Name;
                await category.ModifyAsync(x => x.Name = newName);
                await ReplyConfirmationAsync(Localization.AdministrationCategoryChannelRenamed, oldName, newName);
            }
            
            [Command("addtextchanneltocategory", "atchtocat")]
            [MemberPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
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
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                    return;
                }

                var category = Context.Guild!.GetCategoryChannel(categoryName);

                if (category is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationCategoryChannelNotFound);
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, category))
                {
                    await ReplyErrorAsync(Localization.AdministrationCategoryChannelNoViewPermission);
                    return;
                }

                await channel.ModifyAsync(x => x.Parent = category);
                await ReplyConfirmationAsync(Localization.AdministrationTextChannelAddedToCategory, channel.Name, category.Name);
            }
            
            [Command("addvoicechanneltocategory", "avchtocat")]
            [MemberPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
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
                    await ReplyErrorAsync(Localization.AdministrationVoiceChannelNotFound);
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationVoiceChannelNoViewPermission);
                    return;
                }

                var category = Context.Guild!.GetCategoryChannel(categoryName);

                if (category is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationCategoryChannelNotFound);
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, category))
                {
                    await ReplyErrorAsync(Localization.AdministrationCategoryChannelNoViewPermission);
                    return;
                }

                await channel.ModifyAsync(x => x.Parent = category);
                await ReplyConfirmationAsync(Localization.AdministrationVoiceChannelAddedToCategory, channel.Name, category.Name);
            }
        }
    }
}