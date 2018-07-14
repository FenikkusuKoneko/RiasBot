using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class ChannelsCommands : RiasSubmodule
        {
            public readonly CommandHandler _ch;
            public readonly CommandService _service;

            public ChannelsCommands(CommandHandler ch, CommandService service)
            {
                _ch = ch;
                _service = service;
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task CreateChannel(string type, [Remainder]string name)
            {
                type = type.ToLowerInvariant();
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                switch (type)
                {
                    case "text":
                        await Context.Guild.CreateTextChannelAsync(name).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {type} channel {Format.Bold(name)} was created successfully.").ConfigureAwait(false);
                        break;
                    case "voice":
                        await Context.Guild.CreateVoiceChannelAsync(name).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {type} channel {Format.Bold(name)} was created successfully.").ConfigureAwait(false);
                        break;
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteChannel(string type, [Remainder]string name)
            {
                type = type.ToLowerInvariant();

                IGuildChannel channel = null;
                switch (type)
                {
                    case "text":
                        name = name.Replace(" ", "-");
                        channel = (await Context.Guild.GetTextChannelsAsync()).Where(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
                        break;
                    case "voice":
                        channel = (await Context.Guild.GetVoiceChannelsAsync()).Where(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
                        break;
                    default:
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the type of the channel is invalid. Must be text or voice.").ConfigureAwait(false);
                        return;
                }
                if (channel != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(channel);
                    if (permissions.ViewChannel)
                    {
                        if (channel.Id != Context.Channel.Id)
                        {
                            await channel.DeleteAsync();
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {type} channel {Format.Bold(name)} was deleted successfully.").ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you can't delete the channel in that this command is executed.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I don't have the permission to view that channel.");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the channel.").ConfigureAwait(false);
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
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateCategoryAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} category {Format.Bold(name)} was created successfully.");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteCategory([Remainder]string name)
            {
                var category = (await Context.Guild.GetCategoriesAsync().ConfigureAwait(false)).Where(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
                if (category != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(category);
                    if (permissions.ViewChannel)
                    {
                        await category.DeleteAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} category channel {Format.Bold(name)} was deleted successfully.");
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I don't have the permission to view that category channel.");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the category channel.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameChannel(string type, [Remainder]string channels)
            {
                IGuildChannel channel = null;
                var chns = channels.Split("->");
                var oldName = chns[0].TrimEnd();
                var newName = chns[1].TrimStart();
                switch (type)
                {
                    case "text":
                        oldName = oldName.Replace(" ", "-");
                        channel = (await Context.Guild.GetTextChannelsAsync().ConfigureAwait(false)).Where(x => x.Name.ToLowerInvariant() == oldName.ToLowerInvariant()).FirstOrDefault();
                        break;
                    case "voice":
                        channel = (await Context.Guild.GetVoiceChannelsAsync().ConfigureAwait(false)).Where(x => x.Name.ToLowerInvariant() == oldName.ToLowerInvariant()).FirstOrDefault();
                        break;
                    default:
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the type of the channel is invalid. Must be text or voice.").ConfigureAwait(false);
                        return;
                }
                if (channel != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(channel);
                    if (permissions.ViewChannel)
                    {
                        await channel.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name of the {type} channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I don't have the permission to view that channel.");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the channel.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameCategory([Remainder]string categories)
            {
                var cats = categories.Split("->");
                var oldName = cats[0].TrimEnd();
                var newName = cats[1].TrimStart();
                var category = (await Context.Guild.GetCategoriesAsync().ConfigureAwait(false)).Where(x => x.Name.ToLowerInvariant() == oldName.ToLowerInvariant()).FirstOrDefault();
                if (category != null)
                {
                    var permissions = (await Context.Guild.GetCurrentUserAsync()).GetPermissions(category);
                    if (permissions.ViewChannel)
                    {
                        await category.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name of the category channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I don't have the permission to view that category channel.");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the category.").ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelTopic()
            {
                var channel = (ITextChannel)Context.Channel;
                if (!String.IsNullOrEmpty(channel.Topic))
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithTitle("This channel's topic");
                    embed.WithDescription(channel.Topic);
                    await Context.Channel.SendMessageAsync(embed: embed.Build());
                }
                else
                    await Context.Channel.SendConfirmationEmbed("No topic set on this channel.");
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
                if (String.IsNullOrEmpty(topic))
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} channel's topic set to {Format.Bold("null")}");
                else
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} channel's topic set to {Format.Bold(topic)}");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task SetNSFWChannel(ITextChannel channel = null)
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
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} this channel is not NSFW anymore.");
                        else
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {Format.Bold(channel.Name)} is not NSFW anymore.");
                    }
                    else
                    {
                        await channel.ModifyAsync(x => x.IsNsfw = true).ConfigureAwait(false);
                        if (channel.Id == Context.Channel.Id)
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} this channel is now NSFW.");
                        else
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {Format.Bold(channel.Name)} is now NSFW.");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I don't have the permission to view the channel.");
                }
            }
        }
    }
}
