using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Implementation;

namespace Rias.Modules.Bot
{
    public partial class BotModule
    {
        [Name("Bot")]
        [Group("bot")]
        public class BotGroupModule : RiasModule
        {
            public BotGroupModule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("send")]
            [MasterOnly]
            public async Task SendAsync(string id, [Remainder] string message)
            {
                if (!id.StartsWith("u:", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!ulong.TryParse(id, out var channelId))
                    {
                        await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                        return;
                    }

                    var channel = RiasBot.Client.ShardClients
                        .SelectMany(x => x.Value.Guilds)
                        .SelectMany(x => x.Value.Channels)
                        .FirstOrDefault(x => x.Key == channelId).Value;
                
                    if (channel is null)
                    {
                        await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                        return;
                    }

                    if (channel.Type != ChannelType.Text && channel.Type != ChannelType.News)
                    {
                        await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                        return;
                    }

                    var permissions = channel.Guild.CurrentMember.PermissionsIn(channel);
                    if (!permissions.HasPermission(Permissions.AccessChannels))
                    {
                        await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                        return;
                    }

                    if (!permissions.HasPermission(Permissions.SendMessages))
                    {
                        await ReplyErrorAsync(Localization.BotTextChannelNoSendMessagesPermission);
                        return;
                    }

                    switch (RiasUtilities.TryParseMessage(message, out var customMessage))
                    {
                        case true when string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null:
                            await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                            return;
                        case true:
                            await channel.SendMessageAsync(customMessage.Content, customMessage.Embed);
                            break;
                        default:
                            await channel.SendMessageAsync(message);
                            break;
                    }

                    await ReplyConfirmationAsync(Localization.BotMessageSent);
                }
                else
                {
                    DiscordMember member;
                    if (ulong.TryParse(id[2..], out var userId) && RiasBot.Members.TryGetValue(userId, out var m))
                    {
                        member = m;
                    }
                    else
                    {
                        await ReplyErrorAsync(Localization.AdministrationUserNotFound);
                        return;
                    }
                
                    if (member.IsBot)
                    {
                        await ReplyErrorAsync(Localization.BotUserIsBot);
                        return;
                    }

                    try
                    {
                        switch (RiasUtilities.TryParseMessage(message, out var customMessage))
                        {
                            case true when string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null:
                                await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                                return;
                            case true:
                                await member.SendMessageAsync(customMessage.Content, customMessage.Embed);
                                break;
                            default:
                                await member.SendMessageAsync(message);
                                break;
                        }

                        await ReplyConfirmationAsync(Localization.BotMessageSent);
                    }
                    catch
                    {
                        await ReplyErrorAsync(Localization.BotUserMessageNotSent);
                    }
                }
            }

            [Command("edit")]
            [MasterOnly]
            public async Task EditAsync(ulong channelId, ulong messageId, [Remainder] string message)
            {
                var channel = RiasBot.Client.ShardClients
                    .SelectMany(x => x.Value.Guilds)
                    .SelectMany(x => x.Value.Channels)
                    .FirstOrDefault(x => x.Key == channelId).Value;
            
                if (channel is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNotFound);
                    return;
                }

                if (channel.Type != ChannelType.Text && channel.Type != ChannelType.News)
                {
                    await ReplyErrorAsync(Localization.BotChannelNotTextChannel);
                    return;
                }
            
                var permissions = channel.Guild.CurrentMember.PermissionsIn(channel);
                if (!permissions.HasPermission(Permissions.AccessChannels))
                {
                    await ReplyErrorAsync(Localization.AdministrationTextChannelNoViewPermission);
                    return;
                }

                if (!permissions.HasPermission(Permissions.SendMessages))
                {
                    await ReplyErrorAsync(Localization.BotTextChannelNoSendMessagesPermission);
                    return;
                }

                var discordMessage = await channel.GetMessageAsync(messageId);
                if (discordMessage is null)
                {
                    await ReplyErrorAsync(Localization.BotMessageNotFound);
                    return;
                }

                if (discordMessage.MessageType != MessageType.Default)
                {
                    await ReplyErrorAsync(Localization.BotMessageNotUserMessage);
                    return;
                }

                if (discordMessage.Author.Id != channel.Guild.CurrentMember.Id)
                {
                    await ReplyErrorAsync(Localization.BotMessageNotSelf);
                    return;
                }

                switch (RiasUtilities.TryParseMessage(message, out var customMessage))
                {
                    case true when string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null:
                        await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                        return;
                    case true:
                        await discordMessage.ModifyAsync(customMessage.Content, customMessage.Embed?.Build() ?? (Optional<DiscordEmbed>) default);
                        break;
                    default:
                        await discordMessage.ModifyAsync(message);
                        break;
                }

                await ReplyConfirmationAsync(Localization.BotMessageEdited);
            }
        }
    }
}