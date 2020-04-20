using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    [Name("Administration")]
    public partial class AdministrationModule : RiasModule
    {
        private readonly HttpClient _httpClient;

        public AdministrationModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }
        
        [Command("setgreet"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator), BotPermission(Permission.ManageWebhooks),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task SetGreetAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            if (string.IsNullOrEmpty(guildDb.GreetMessage))
            {
                await ReplyErrorAsync(Localization.AdministrationGreetMessageNotSet);
                return;
            }
            
            var webhook = guildDb.GreetWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.GreetWebhookId) : null;
            guildDb.GreetNotification = !guildDb.GreetNotification;
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
            await using var stream = await _httpClient.GetStreamAsync(currentMember.GetAvatarUrl());
            await using var webhookAvatar = new MemoryStream();
            await stream.CopyToAsync(webhookAvatar);
            webhookAvatar.Position = 0;
            
            webhook = await ((CachedTextChannel) Context.Channel).CreateWebhookAsync(currentMember.Name, webhookAvatar);
            guildDb.GreetWebhookId = webhook.Id;
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.AdministrationGreetEnabled, guildDb.GreetMessage);
        }
        
        [Command("greetmessage"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task GreetMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync(Localization.AdministrationGreetMessageLengthLimit, 1500);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            guildDb.GreetMessage = message;
            await DbContext.SaveChangesAsync();

            await ReplyConfirmationAsync(Localization.AdministrationGreetMessageSet);
        }
        
        [Command("setbye"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator), BotPermission(Permission.ManageWebhooks),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task SetByeAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            if (string.IsNullOrEmpty(guildDb.ByeMessage))
            {
                await ReplyErrorAsync(Localization.AdministrationByeMessageNotSet);
                return;
            }
            
            var webhook = guildDb.ByeWebhookId > 0 ? await Context.Guild!.GetWebhookAsync(guildDb.ByeWebhookId) : null;
            guildDb.ByeNotification = !guildDb.ByeNotification;
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
            await using var stream = await _httpClient.GetStreamAsync(currentMember.GetAvatarUrl());
            await using var webhookAvatar = new MemoryStream();
            await stream.CopyToAsync(webhookAvatar);
            webhookAvatar.Position = 0;
            
            webhook = await ((CachedTextChannel) Context.Channel).CreateWebhookAsync(currentMember.Name, webhookAvatar);
            guildDb.ByeWebhookId = webhook.Id;
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.AdministrationByeEnabled, guildDb.ByeMessage);
        }
        
        [Command("byemessage"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task ByeMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync(Localization.AdministrationByeMessageLengthLimit, 1500);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            guildDb.ByeMessage = message;
            await DbContext.SaveChangesAsync();

            await ReplyConfirmationAsync(Localization.AdministrationByeMessageSet);
        }
        
        [Command("setmodlog"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task SetModLogAsync()
        {
            var modLogSet = false;
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            if (guildDb.ModLogChannelId != Context.Channel.Id)
            {
                guildDb.ModLogChannelId = Context.Channel.Id;
                modLogSet = true;
            }
            else
            {
                guildDb.ModLogChannelId = 0;
            }

            await DbContext.SaveChangesAsync();
            
            if (modLogSet)
                await ReplyConfirmationAsync(Localization.AdministrationModLogEnabled);
            else
                await ReplyConfirmationAsync(Localization.AdministrationModLogDisabled);
        }
    }
}