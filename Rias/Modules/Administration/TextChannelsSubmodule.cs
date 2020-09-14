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
        [Name("Text Channels")]
        public class TextChannelsSubmodule : RiasModule
        {
            public TextChannelsSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            [Command("createtextchannel")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
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

            [Command("deletetextchannel")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteTextChannelAsync([TextChannel, Remainder] DiscordChannel channel)
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

            [Command("renametextchannel")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
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

            [Command("channeltopic")]
            [Context(ContextType.Guild)]
            public async Task ChannelTopicAsync()
            {
                if (string.IsNullOrEmpty(Context.Channel.Topic))
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNoTopic);
                    return;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.AdministrationChannelTopicTitle),
                    Description = Context.Channel.Topic
                };

                await ReplyAsync(embed);
            }

            [Command("setchanneltopic")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetChannelTopic([Remainder] string? topic = null)
            {
                if (!string.IsNullOrEmpty(topic) && topic.Length > 1024)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelTopicLengthLimit);
                    return;
                }

                await Context.Channel.ModifyAsync(x => x.Topic = topic);
                if (string.IsNullOrEmpty(topic))
                {
                    await ReplyConfirmationAsync(Localization.AdministrationChannelTopicRemoved);
                }
                else
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = GetText(Localization.AdministrationChannelTopicSet),
                        Description = topic
                    };

                    await ReplyAsync(embed);
                }
            }

            [Command("setnsfwchannel")]
            [Context(ContextType.Guild)]
            [UserPermission(Permissions.ManageChannels)]
            [BotPermission(Permissions.ManageChannels)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task SetNsfwChannelAsync([TextChannel, Remainder] DiscordChannel? channel = null)
            {
                channel ??= Context.Channel;

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                }

                if (channel.IsNSFW)
                {
                    await channel.ModifyAsync(x => x.Nsfw = false);
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync(Localization.AdministrationCurrentChannelNsfwDisabled);
                    else
                        await ReplyConfirmationAsync(Localization.AdministrationChannelNsfwDisabled, channel.Name);
                }
                else
                {
                    await channel.ModifyAsync(x => x.Nsfw = true);
                    if (channel.Id == Context.Channel.Id)
                        await ReplyConfirmationAsync(Localization.AdministrationCurrentChannelNsfwEnabled);
                    else
                        await ReplyConfirmationAsync(Localization.AdministrationChannelNsfwEnabled, channel.Name);
                }
            }
        }
    }
}