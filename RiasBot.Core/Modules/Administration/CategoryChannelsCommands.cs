using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class CategoryChannelsCommands : RiasSubmodule
        {
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task CreateCategory([Remainder]string name)
            {
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationMessageAsync("The name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateCategoryAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationMessageAsync($"Category {Format.Bold(name)} was created successfully");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteCategory([Remainder]string name)
            {
                var category = (await Context.Guild.GetCategoriesAsync().ConfigureAwait(false)).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
                if (category != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(category);
                    if (permissions.ViewChannel)
                    {
                        await category.DeleteAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"Category channel {Format.Bold(category.Name)} was deleted successfully");
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("I don't have the permission to view that category channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the category channel").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameCategory([Remainder]string names)
            {
                var namesSplit = names.Split("->");
                var oldName = namesSplit[0].TrimEnd();
                var newName = namesSplit[1].TrimStart();
                var category = (await Context.Guild.GetCategoriesAsync().ConfigureAwait(false)).FirstOrDefault(x => string.Equals(x.Name, oldName, StringComparison.InvariantCultureIgnoreCase));
                if (category != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(category);
                    if (permissions.ViewChannel)
                    {
                        oldName = category.Name;
                        await category.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"The name of the category channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("I don't have the permission to view that category channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the category").ConfigureAwait(false);
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task AddTextChannelToCategory([Remainder] string names)
            {
                var namesSplit = names.Split("->");
                var channelName = namesSplit[0].TrimEnd();
                var categoryName = namesSplit[1].TrimStart();
                var channel = (await Context.Guild.GetTextChannelsAsync()).FirstOrDefault(x => x.Name.Equals(channelName, StringComparison.InvariantCultureIgnoreCase));
                if (channel != null)
                {
                    var category = (await Context.Guild.GetCategoriesAsync()).FirstOrDefault(x => x.Name.Equals(categoryName, StringComparison.InvariantCultureIgnoreCase));
                    if (category != null)
                    {
                        await channel.ModifyAsync(x => x.CategoryId = category.Id).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"Text channel {Format.Bold(channel.Name)} was added to category {Format.Bold(category.Name)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("I couldn't find the category").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the channel").ConfigureAwait(false);
                }
            }
            
            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task AddVoiceChannelToCategory([Remainder] string names)
            {
                var namesSplit = names.Split("->");
                var channelName = namesSplit[0].TrimEnd();
                var categoryName = namesSplit[1].TrimStart();
                var channel = (await Context.Guild.GetVoiceChannelsAsync()).FirstOrDefault(x => x.Name.Equals(channelName, StringComparison.InvariantCultureIgnoreCase));
                if (channel != null)
                {
                    var category = (await Context.Guild.GetCategoriesAsync()).FirstOrDefault(x => x.Name.Equals(categoryName, StringComparison.InvariantCultureIgnoreCase));
                    if (category != null)
                    {
                        await channel.ModifyAsync(x => x.CategoryId = category.Id).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"Voice channel {Format.Bold(channel.Name)} was added to category {Format.Bold(category.Name)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("I couldn't find the category").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the channel").ConfigureAwait(false);
                }
            }
        }
    }
}
