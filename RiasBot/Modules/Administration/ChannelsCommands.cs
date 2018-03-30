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
                name = name.Replace(" ", "-");
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                switch (type)
                {
                    case "text":
                        await Context.Guild.CreateTextChannelAsync(name).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"Text channel {Format.Bold(name)} was created successfully.").ConfigureAwait(false);
                        break;
                    case "voice":
                        await Context.Guild.CreateVoiceChannelAsync(name).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"Voice channel {Format.Bold(name)} was created successfully.").ConfigureAwait(false);
                        break;
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteChannel(string type, [Remainder]string name)
            {
                name = name.Replace(" ", "-");
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters").ConfigureAwait(false);
                    return;
                }
                IGuildChannel channel = null;
                switch (type)
                {
                    case "text":
                        channel = (await Context.Guild.GetTextChannelsAsync().ConfigureAwait(false)).Where(x => x.Name == name).FirstOrDefault();
                        break;
                    case "voice":
                        channel = (await Context.Guild.GetVoiceChannelsAsync().ConfigureAwait(false)).Where(x => x.Name == name).FirstOrDefault();
                        break;
                }

                if (channel != null)
                {
                    await channel.DeleteAsync();
                    await Context.Channel.SendConfirmationEmbed($"Text channel {Format.Bold(name)} was deleted successfully.").ConfigureAwait(false);
                }
                else
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} I couldn't find the channel.").ConfigureAwait(false);
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task CreateCategory([Remainder]string name)
            {
                name = name.ToUpper();
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateCategoryAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"Category {Format.Bold(name)} was created successfully.");
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task DeleteCategory([Remainder]string name)
            {
                name = name.ToUpper();
                if (name.Length < 2 || name.Length > 100)
                {
                    await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                var category = (await Context.Guild.GetCategoriesAsync().ConfigureAwait(false)).Where(x => x.Name == name).FirstOrDefault();
                await category.DeleteAsync().ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"Category {Format.Bold(name)} was deleted.");
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameChannel(string type, [Remainder]string channels)
            {
                try
                {
                    IGuildChannel channel = null;
                    var chns = channels.Split("->");
                    string oldName = chns[0].TrimEnd();
                    string newName = chns[1].TrimStart();

                    switch (type)
                    {
                        case "text":
                            channel = (await Context.Guild.GetTextChannelsAsync().ConfigureAwait(false)).Where(x => x.Name == oldName).FirstOrDefault();
                            await channel.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"The name of text channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully.").ConfigureAwait(false);
                            break;
                        case "voice":
                            channel = (await Context.Guild.GetVoiceChannelsAsync().ConfigureAwait(false)).Where(x => x.Name == oldName).FirstOrDefault();
                            await channel.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"The name of voice channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully.").ConfigureAwait(false);
                            break;
                        default:
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the channel.");
                            break;
                    }
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the channel.");
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameCategory([Remainder]string categories)
            {
                try
                {
                    ICategoryChannel category = null;
                    var cats = categories.Split("->");
                    string oldName = cats[0].TrimEnd().ToUpper();
                    string newName = cats[1].TrimStart().ToUpper();

                    category = (await Context.Guild.GetCategoriesAsync().ConfigureAwait(false)).Where(x => x.Name.ToUpper() == oldName).FirstOrDefault();

                    if (category != null)
                    {
                        await category.ModifyAsync(x => x.Name = newName).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"The name of category {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully.").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the category.").ConfigureAwait(false);
                    }
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the category.");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelTopic()
            {
                var channel = (ITextChannel)Context.Channel;
                if (!String.IsNullOrEmpty(channel.Topic))
                    await Context.Channel.SendConfirmationEmbed("This channel's topic: " + Format.Bold(channel.Topic));
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
        }
    }
}
