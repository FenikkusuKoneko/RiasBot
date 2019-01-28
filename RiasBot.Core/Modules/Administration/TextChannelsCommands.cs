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
        public class TextChannelsCommands : RiasSubmodule
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
                    await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateTextChannelAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationMessageAsync($"Text channel {Format.Bold(name)} was created successfully").ConfigureAwait(false);
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
                            await Context.Channel.SendConfirmationMessageAsync($"Text channel {Format.Bold(channel.Name)} was deleted successfully").ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorMessageAsync("You can't delete the channel were this command is executed").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("I don't have the permission to view that channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the channel").ConfigureAwait(false);
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
                        await Context.Channel.SendConfirmationMessageAsync($"The name of the text channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("I don't have the permission to view that channel");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the channel.").ConfigureAwait(false);
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
                    await Context.Channel.SendConfirmationMessageAsync("No topic set on this channel");
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
                    await Context.Channel.SendConfirmationMessageAsync($"Channel's topic set to {Format.Bold("null")}");
                else
                {
                    await Context.Channel.SendConfirmationMessageAsync($"Channel's topic set to {Format.Bold(topic)}");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task SetNsfwChannel(ITextChannel channel = null)
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
                            await Context.Channel.SendConfirmationMessageAsync("This channel is not NSFW anymore.");
                        else
                            await Context.Channel.SendConfirmationMessageAsync($"{Format.Bold(channel.Name)} is not NSFW anymore.");
                    }
                    else
                    {
                        await channel.ModifyAsync(x => x.IsNsfw = true).ConfigureAwait(false);
                        if (channel.Id == Context.Channel.Id)
                            await Context.Channel.SendConfirmationMessageAsync("This channel is now NSFW.");
                        else
                            await Context.Channel.SendConfirmationMessageAsync($"{Format.Bold(channel.Name)} is now NSFW.");
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("I don't have the permission to view that channel");
                }
            }
        }
    }
}
