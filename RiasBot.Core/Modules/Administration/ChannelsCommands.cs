using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using System;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class ChannelsCommands : RiasSubmodule
        {

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task CreateTextChannel([Remainder]string name)
            {
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateTextChannelAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"Text channel {Format.Bold(name)} was created successfully").ConfigureAwait(false);
            }
            
            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task CreateVoiceChannel([Remainder]string name)
            {
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateVoiceChannelAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"Voice channel {Format.Bold(name)} was created successfully").ConfigureAwait(false);
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteTextChannel([Remainder]string name)
            {
                name = name.Replace(" ", "-");
                var channel = (await Context.Guild.GetTextChannelsAsync()).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
                if (channel != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(channel);
                    if (permissions.ViewChannel)
                    {
                        if (channel.Id != Context.Channel.Id)
                        {
                            await channel.DeleteAsync();
                            await Context.Channel.SendConfirmationEmbed($"Text channel {Format.Bold(channel.Name)} was deleted successfully").ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed("You can't delete the channel were this command is executed").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I don't have the permission to view that channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the channel").ConfigureAwait(false);
                }
            }
            
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteVoiceChannel([Remainder]string name)
            {
                var channel = (await Context.Guild.GetVoiceChannelsAsync()).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
                if (channel != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(channel);
                    if (permissions.ViewChannel)
                    {
                        await channel.DeleteAsync();
                        await Context.Channel.SendConfirmationEmbed($"Voice channel {Format.Bold(channel.Name)} was deleted successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I don't have the permission to view that channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the channel").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task CreateCategory([Remainder]string name)
            {
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed("The name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateCategoryAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"Category {Format.Bold(name)} was created successfully");
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
                        await Context.Channel.SendConfirmationEmbed($"Category channel {Format.Bold(category.Name)} was deleted successfully");
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I don't have the permission to view that category channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the category channel").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameTextChannel([Remainder]string names)
            {
                var namesSplit = names.Split("->");
                var oldName = namesSplit[0].TrimEnd().Replace(" ", "-");
                var newName = namesSplit[1].TrimStart();
                var channel = (await Context.Guild.GetTextChannelsAsync().ConfigureAwait(false)).FirstOrDefault(x => string.Equals(x.Name, oldName, StringComparison.InvariantCultureIgnoreCase));
                if (channel != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(channel);
                    if (permissions.ViewChannel)
                    {
                        oldName = channel.Name;
                        await channel.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"The name of the text channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I don't have the permission to view that channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the channel.").ConfigureAwait(false);
                }
            }
            
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameVoiceChannel([Remainder]string names)
            {
                var namesSplit = names.Split("->");
                var oldName = namesSplit[0].TrimEnd().Replace(" ", "-");
                var newName = namesSplit[1].TrimStart();
                var channel = (await Context.Guild.GetVoiceChannelsAsync().ConfigureAwait(false)).FirstOrDefault(x => string.Equals(x.Name, oldName, StringComparison.InvariantCultureIgnoreCase));
                if (channel != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(channel);
                    if (permissions.ViewChannel)
                    {
                        oldName = channel.Name;
                        await channel.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"The name of the voice channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I don't have the permission to view that channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the channel").ConfigureAwait(false);
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
                        await Context.Channel.SendConfirmationEmbed($"The name of the category channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I don't have the permission to view that category channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the category").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelTopic()
            {
                var channel = (ITextChannel)Context.Channel;
                if (!string.IsNullOrEmpty(channel.Topic))
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithTitle("This channel's topic");
                    embed.WithDescription(channel.Topic);
                    await Context.Channel.SendMessageAsync(embed: embed.Build());
                }
                else
                    await Context.Channel.SendConfirmationEmbed("No topic set on this channel");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task SetChannelTopic([Remainder]string topic = null)
            {
                var channel = (ITextChannel)Context.Channel;
                await channel.ModifyAsync(x => x.Topic = topic);
                if (string.IsNullOrEmpty(topic))
                    await Context.Channel.SendConfirmationEmbed($"Channel's topic set to {Format.Bold("null")}");
                else
                {
                    await Context.Channel.SendConfirmationEmbed($"Channel's topic set to {Format.Bold(topic)}");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task SetNfwChannel(ITextChannel channel = null)
            {
                if (channel is null)
                {
                    channel = (ITextChannel)Context.Channel;
                }
                var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(channel);
                if (permissions.ViewChannel)
                {
                    if (channel.IsNsfw)
                    {
                        await channel.ModifyAsync(x => x.IsNsfw = false).ConfigureAwait(false);
                        if (channel.Id == Context.Channel.Id)
                            await Context.Channel.SendConfirmationEmbed("This channel is not NSFW anymore.");
                        else
                            await Context.Channel.SendConfirmationEmbed($"{Format.Bold(channel.Name)} is not NSFW anymore.");
                    }
                    else
                    {
                        await channel.ModifyAsync(x => x.IsNsfw = true).ConfigureAwait(false);
                        if (channel.Id == Context.Channel.Id)
                            await Context.Channel.SendConfirmationEmbed("This channel is now NSFW.");
                        else
                            await Context.Channel.SendConfirmationEmbed($"{Format.Bold(channel.Name)} is now NSFW.");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I don't have the permission to view that channel");
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
                        await Context.Channel.SendConfirmationEmbed($"Text channel {Format.Bold(channel.Name)} was added to category {Format.Bold(category.Name)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I couldn't find the category").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the channel").ConfigureAwait(false);
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
                        await Context.Channel.SendConfirmationEmbed($"Voice channel {Format.Bold(channel.Name)} was added to category {Format.Bold(category.Name)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("I couldn't find the category").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the channel").ConfigureAwait(false);
                }
            }
        }
    }
}
