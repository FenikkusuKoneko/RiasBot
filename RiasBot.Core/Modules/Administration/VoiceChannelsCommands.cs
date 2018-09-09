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
        public class VoiceChannelsCommands : RiasSubmodule
        {
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
                    await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} the name length must be between 2 and 100 characters");
                    return;
                }
                await Context.Guild.CreateVoiceChannelAsync(name).ConfigureAwait(false);
                await Context.Channel.SendConfirmationMessageAsync($"Voice channel {Format.Bold(name)} was created successfully").ConfigureAwait(false);
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
                        await Context.Channel.SendConfirmationMessageAsync($"Voice channel {Format.Bold(channel.Name)} was deleted successfully").ConfigureAwait(false);
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

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            [RequireBotPermission(GuildPermission.ManageChannels)]
            [RequireContext(ContextType.Guild)]
            public async Task RenameVoiceChannel([Remainder] string names)
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
                        await Context.Channel.SendConfirmationMessageAsync($"The name of the voice channel {Format.Bold(oldName)} was renamed to {Format.Bold(newName)} successfully").ConfigureAwait(false);
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
        }
    }
}
