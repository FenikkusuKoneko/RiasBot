using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Administration
{
    [Name("Administration")]
    public partial class AdministrationModule : RiasModule
    {
        public AdministrationModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        
        [Command("setgreet", "greet")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        [BotPermission(Permissions.ManageWebhooks)]
        [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task SetGreetAsync([TextChannel, Remainder] DiscordChannel? channel = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            if (string.IsNullOrEmpty(guildDb.GreetMessage))
            {
                await ReplyErrorAsync(Localization.AdministrationGreetMessageNotSet);
                return;
            }
            
            var webhook = guildDb.GreetWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.GreetWebhookId) : null;
            if (channel is null)
                guildDb.GreetNotification = !guildDb.GreetNotification;
            else
                guildDb.GreetNotification = true;
            
            if (!guildDb.GreetNotification)
            {
                if (webhook != null)
                    await webhook.DeleteAsync();

                guildDb.GreetWebhookId = 0;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.AdministrationGreetDisabled);

                return;
            }
            
            var currentMember = Context.CurrentMember!;
            await using var stream = await HttpClient.GetStreamAsync(currentMember.GetAvatarUrl(ImageFormat.Auto));
            await using var webhookAvatar = new MemoryStream();
            await stream.CopyToAsync(webhookAvatar);
            webhookAvatar.Position = 0;

            if (webhook is null)
            {
                if (channel is null)
                    webhook = await Context.Channel.CreateWebhookAsync(currentMember.Username, webhookAvatar);
                else
                    webhook = await channel.CreateWebhookAsync(currentMember.Username, webhookAvatar);
            }
            else if (channel is not null)
            {
                await webhook.ModifyAsync(currentMember.Username, webhookAvatar, channel.Id);
            }

            guildDb.GreetWebhookId = webhook.Id;
            await DbContext.SaveChangesAsync();

            var greetMessage = BotService.ReplacePlaceholders(Context.User, guildDb.GreetMessage);
            if (RiasUtilities.TryParseMessage(greetMessage, out var customMessage))
            {
                var content = (channel is null
                                  ? GetText(Localization.AdministrationGreetEnabled)
                                  : GetText(Localization.AdministrationGreetEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationGreetMessage)}\n\n{customMessage.Content}";
                await Context.Channel.SendMessageAsync(content, customMessage.Embed);
            }
            else
            {
                var content = (channel is null
                                  ? GetText(Localization.AdministrationGreetEnabled)
                                  : GetText(Localization.AdministrationGreetEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationGreetMessage)}\n\n{greetMessage}";
                await Context.Channel.SendMessageAsync(content);
            }
        }
        
        [Command("greetmessage", "greetmsg")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        [BotPermission(Permissions.ManageWebhooks)]
        public async Task GreetMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync(Localization.AdministrationGreetMessageLengthLimit, 1500);
                return;
            }
            
            var greetMessage = BotService.ReplacePlaceholders(Context.User, message);
            var greetMessageParsed = RiasUtilities.TryParseMessage(greetMessage, out var customMessage);
            if (greetMessageParsed && string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null)
            {
                await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            guildDb.GreetMessage = message;
            await DbContext.SaveChangesAsync();
            
            var webhook = guildDb.GreetWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.GreetWebhookId) : null;
            var channel = Context.Guild!.GetChannel(webhook?.ChannelId ?? 0);

            if (greetMessageParsed)
            {
                var content = $"{GetText(Localization.AdministrationGreetMessageSet)}\n"
                              + (channel is null
                                  ? GetText(Localization.AdministrationGreetDisabled)
                                  : channel.Id == Context.Channel.Id
                                      ? GetText(Localization.AdministrationGreetEnabled)
                                      : GetText(Localization.AdministrationGreetEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationGreetMessage)}\n\n{customMessage.Content}";
                await Context.Channel.SendMessageAsync(content, customMessage.Embed);
            }
            else
            {
                var content = $"{GetText(Localization.AdministrationGreetMessageSet)}\n"
                              + (channel is null
                                  ? GetText(Localization.AdministrationGreetDisabled)
                                  : channel.Id == Context.Channel.Id
                                      ? GetText(Localization.AdministrationGreetEnabled)
                                      : GetText(Localization.AdministrationGreetEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationGreetMessage)}\n\n{greetMessage}";
                await Context.Channel.SendMessageAsync(content);
            }
        }
        
        [Command("setbye", "bye")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        [BotPermission(Permissions.ManageWebhooks)]
        [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task SetByeAsync([TextChannel, Remainder] DiscordChannel? channel = null)
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            if (string.IsNullOrEmpty(guildDb.ByeMessage))
            {
                await ReplyErrorAsync(Localization.AdministrationByeMessageNotSet);
                return;
            }
            
            var webhook = guildDb.ByeWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.ByeWebhookId) : null;
            if (channel is null)
                guildDb.ByeNotification = !guildDb.ByeNotification;
            else
                guildDb.ByeNotification = true;
            
            if (!guildDb.ByeNotification)
            {
                if (webhook != null)
                    await webhook.DeleteAsync();

                guildDb.ByeWebhookId = 0;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.AdministrationByeDisabled);
                
                return;
            }
            
            var currentMember = Context.CurrentMember!;
            await using var stream = await HttpClient.GetStreamAsync(currentMember.GetAvatarUrl(ImageFormat.Auto));
            await using var webhookAvatar = new MemoryStream();
            await stream.CopyToAsync(webhookAvatar);
            webhookAvatar.Position = 0;

            if (webhook is null)
            {
                if (channel is null)
                    webhook = await Context.Channel.CreateWebhookAsync(currentMember.Username, webhookAvatar);
                else
                    webhook = await channel.CreateWebhookAsync(currentMember.Username, webhookAvatar);
            }
            else if (channel is not null)
            {
                await webhook.ModifyAsync(currentMember.Username, webhookAvatar, channel.Id);
            }
            
            guildDb.ByeWebhookId = webhook.Id;
            await DbContext.SaveChangesAsync();
            
            var byeMessage = BotService.ReplacePlaceholders(Context.User, guildDb.ByeMessage);
            if (RiasUtilities.TryParseMessage(byeMessage, out var customMessage))
            {
                var content = (channel is null
                                  ? GetText(Localization.AdministrationByeEnabled)
                                  : GetText(Localization.AdministrationByeEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationByeMessage)}\n\n{customMessage.Content}";
                await Context.Channel.SendMessageAsync(content, customMessage.Embed);
            }
            else
            {
                var content = (channel is null
                                  ? GetText(Localization.AdministrationByeEnabled)
                                  : GetText(Localization.AdministrationByeEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationByeMessage)}\n\n{byeMessage}";
                await Context.Channel.SendMessageAsync(content);
            }
        }
        
        [Command("byemessage", "byemsg")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task ByeMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync(Localization.AdministrationByeMessageLengthLimit, 1500);
                return;
            }
            
            var byeMessage = BotService.ReplacePlaceholders(Context.User, message);
            var byeMessageParsed = RiasUtilities.TryParseMessage(byeMessage, out var customMessage);
            if (byeMessageParsed && string.IsNullOrEmpty(customMessage.Content) && customMessage.Embed is null)
            {
                await ReplyErrorAsync(Localization.AdministrationNullCustomMessage);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });
            guildDb.ByeMessage = message;
            await DbContext.SaveChangesAsync();
            
            var webhook = guildDb.ByeWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.ByeWebhookId) : null;
            var channel = Context.Guild!.GetChannel(webhook?.ChannelId ?? 0);

            if (byeMessageParsed)
            {
                var content = $"{GetText(Localization.AdministrationByeMessageSet)}\n"
                              + (channel is null
                                  ? GetText(Localization.AdministrationByeDisabled)
                                  : channel.Id == Context.Channel.Id
                                      ? GetText(Localization.AdministrationByeEnabled)
                                      : GetText(Localization.AdministrationByeEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationByeMessage)}\n\n{customMessage.Content}";
                await Context.Channel.SendMessageAsync(content, customMessage.Embed);
            }
            else
            {
                var content = $"{GetText(Localization.AdministrationByeMessageSet)}\n"
                              + (channel is null
                                  ? GetText(Localization.AdministrationByeDisabled)
                                  : channel.Id == Context.Channel.Id
                                      ? GetText(Localization.AdministrationByeEnabled)
                                      : GetText(Localization.AdministrationByeEnabledChannel, channel.Mention))
                              + $"\n{GetText(Localization.AdministrationByeMessage)}\n\n{byeMessage}";
                await Context.Channel.SendMessageAsync(content);
            }
        }
        
        [Command("setmodlog", "modlog")]
        [Context(ContextType.Guild)]
        [MemberPermission(Permissions.Administrator)]
        public async Task SetModLogAsync([TextChannel, Remainder] DiscordChannel? channel = null)
        {
            var modLogSet = false;
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildEntity { GuildId = Context.Guild!.Id });

            if (channel is not null)
            {
                guildDb.ModLogChannelId = channel.Id;
                modLogSet = true;
            }
            else
            {
                if (guildDb.ModLogChannelId == 0)
                {
                    guildDb.ModLogChannelId = Context.Channel.Id;
                    modLogSet = true;
                }
                else
                {
                    guildDb.ModLogChannelId = 0;
                }
            }

            await DbContext.SaveChangesAsync();

            if (modLogSet)
            {
                if (channel is null)
                    await ReplyConfirmationAsync(Localization.AdministrationModLogEnabled);
                else
                    await ReplyConfirmationAsync(Localization.AdministrationModLogEnabledChannel, channel.Mention);
            }
            else
            {
                await ReplyConfirmationAsync(Localization.AdministrationModLogDisabled);
            }
        }
    }
}