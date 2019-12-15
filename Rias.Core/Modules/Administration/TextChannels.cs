using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Text Channels")]
        public class TextChannels: RiasModule
        {
            public TextChannels(IServiceProvider services) : base(services)
            {
            }

            [Command("createtextchannel"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateTextChannelAsync([Remainder] string name)
            {
                if (name.Length < 1 || name.Length > 100)
                {
                    await ReplyErrorAsync("ChannelNameLengthLimit", 1, 100);
                    return;
                }

                await Context.Guild!.CreateTextChannelAsync(name);
                await ReplyConfirmationAsync("TextChannelCreated", name);
            }

            [Command("deletetextchannel"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteTextChannelAsync([Remainder] SocketTextChannel channel)
            {
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, channel))
                {
                    await ReplyErrorAsync("TextChannelNoViewPermission");
                    return;
                }

                await channel.DeleteAsync();
                if (channel.Id != Context.Channel.Id)
                    await ReplyConfirmationAsync("TextChannelDeleted", channel.Name);
            }

            [Command("renametextchannel"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameTextChannelAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");
                if (namesSplit.Length < 2)
                    return;

                var oldName = namesSplit[0].TrimEnd();
                var newName = namesSplit[1].TrimStart();

                var channel = Context.Guild!.GetTextChannel(oldName);

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

                if (newName.Length < 1 || newName.Length > 100)
                {
                    await ReplyErrorAsync("ChannelNameLengthLimit", 1, 100);
                    return;
                }

                oldName = channel.Name;
                await channel.ModifyAsync(x => x.Name = newName);
                await ReplyConfirmationAsync("TextChannelRenamed", oldName, newName);
            }

            [Command("channeltopic"), Context(ContextType.Guild)]
            public async Task ChannelTopicAsync()
            {
                var channel = (SocketTextChannel) Context.Channel;
                if (string.IsNullOrEmpty(channel.Topic))
                {
                    await ReplyErrorAsync("ChannelNotTopic");
                    return;
                }

                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = GetText("ChannelTopicTitle"),
                    Description = channel.Topic
                };

                await ReplyAsync(embed);
            }

            [Command("setchanneltopic"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetChannelTopic([Remainder] string? topic = null)
            {
                if (!string.IsNullOrEmpty(topic) && topic.Length > 1024)
                {
                    await ReplyErrorAsync("ChannelTopicLengthLimit");
                    return;
                }

                var channel = (SocketTextChannel) Context.Channel;
                await channel.ModifyAsync(x => x.Topic = topic);
                if (string.IsNullOrEmpty(topic))
                {
                    await ReplyConfirmationAsync("ChannelTopicRemoved");
                }
                else
                {
                    var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Title = GetText("ChannelTopicSet"),
                        Description = topic
                    };

                    await ReplyAsync(embed);
                }
            }

            [Command("setnsfwchannel"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetNsfwChannelAsync([Remainder] SocketTextChannel? channel = null)
            {
                if (channel is null)
                {
                    channel = (SocketTextChannel) Context.Channel;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, channel))
                {
                    await ReplyErrorAsync("TextChannelNoViewPermission");
                }

                if (channel.IsNsfw)
                {
                    await channel.ModifyAsync(x => x.IsNsfw = false);
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync("CurrentChannelNsfwDisabled");
                    else
                        await ReplyConfirmationAsync("ChannelNsfwDisabled", channel.Name);
                }
                else
                {
                    await channel.ModifyAsync(x => x.IsNsfw = true);
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync("CurrentChannelNsfwEnabled");
                    else
                        await ReplyConfirmationAsync("ChannelNsfwEnabled", channel.Name);
                }
            }
        }
    }
}