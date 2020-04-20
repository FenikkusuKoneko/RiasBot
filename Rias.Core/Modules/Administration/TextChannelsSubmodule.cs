using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Text Channels")]
        public class TextChannelsSubmodule : RiasModule
        {
            public TextChannelsSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("createtextchannel"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateTextChannelAsync([Remainder] string name)
            {
                if (name.Length < 1 || name.Length > 100)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNameLengthLimit, 1, 100);
                    return;
                }

                await Context.Guild!.CreateTextChannelAsync(name);
                await ReplyConfirmationAsync(Localization.AdministrationTextChannelCreated, name);
            }
            
            [Command("deletetextchannel"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteTextChannelAsync([Remainder] CachedTextChannel channel)
            {
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                    return;
                }

                await channel.DeleteAsync();
                if (channel.Id != Context.Channel.Id)
                    await ReplyConfirmationAsync(Localization.AdministrationTextChannelDeleted, channel.Name);
            }
            
            [Command("renametextchannel"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
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
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                    return;
                }

                if (newName.Length < 1 || newName.Length > 100)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNameLengthLimit, 1, 100);
                    return;
                }

                oldName = channel.Name;
                await channel.ModifyAsync(x => x.Name = newName);
                await ReplyConfirmationAsync(Localization.AdministrationTextChannelRenamed, oldName, newName);
            }
            
            [Command("channeltopic"), Context(ContextType.Guild)]
            public async Task ChannelTopicAsync()
            {
                var channel = (CachedTextChannel) Context.Channel;
                if (string.IsNullOrEmpty(channel.Topic))
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNoTopic);
                    return;
                }

                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationChannelTopicTitle),
                    Description = channel.Topic
                };

                await ReplyAsync(embed);
            }

            [Command("setchanneltopic"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetChannelTopic([Remainder] string? topic = null)
            {
                if (!string.IsNullOrEmpty(topic) && topic.Length > 1024)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelTopicLengthLimit);
                    return;
                }

                var channel = (CachedTextChannel) Context.Channel;
                await channel.ModifyAsync(x => x.Topic = topic);
                if (string.IsNullOrEmpty(topic))
                {
                    await ReplyConfirmationAsync(Localization.AdministrationChannelTopicRemoved);
                }
                else
                {
                    var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = GetText(Localization.AdministrationChannelTopicSet),
                        Description = topic
                    };

                    await ReplyAsync(embed);
                }
            }

            [Command("setnsfwchannel"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetNsfwChannelAsync([Remainder] CachedTextChannel? channel = null)
            {
                if (channel is null)
                {
                    channel = (CachedTextChannel) Context.Channel;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                }

                if (channel.IsNsfw)
                {
                    await channel.ModifyAsync(x => x.IsNsfw = false);
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync(Localization.AdministrationCurrentChannelNsfwDisabled);
                    else
                        await ReplyConfirmationAsync(Localization.AdministrationChannelNsfwDisabled, channel.Name);
                }
                else
                {
                    await channel.ModifyAsync(x => x.IsNsfw = true);
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync(Localization.AdministrationCurrentChannelNsfwEnabled);
                    else
                        await ReplyConfirmationAsync(Localization.AdministrationChannelNsfwEnabled, channel.Name);
                }
            }
        }
    }
}