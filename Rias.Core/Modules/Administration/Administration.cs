using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;

namespace Rias.Core.Modules.Administration
{
    [Name("Administration")]
    public partial class Administration : RiasModule
    {
        private readonly HttpClient _httpClient;

        public Administration(IServiceProvider services) : base(services)
        {
            _httpClient = services.GetRequiredService<HttpClient>();
        }

        [Command("setgreet"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator), BotPermission(GuildPermission.ManageWebhooks),
        Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task SetGreetAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            if (string.IsNullOrEmpty(guildDb.GreetMessage))
            {
                await ReplyErrorAsync("GreetMessageNotSet");
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
                await ReplyConfirmationAsync("GreetDisabled");

                return;
            }
            
            if (webhook != null)
                await webhook.ModifyAsync(x => x.ChannelId = Context.Channel.Id);
            else
                webhook = await CreateWebhookAsync((SocketTextChannel) Context.Channel);

            guildDb.GreetWebhookId = webhook.Id;
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("GreetEnabled", guildDb.GreetMessage);
        }

        [Command("greetmessage"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task GreetMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync("GreetMessageLengthLimit", 1500);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            guildDb.GreetMessage = message;
            await DbContext.SaveChangesAsync();

            await ReplyConfirmationAsync("GreetMessageSet");
        }

        [Command("setbye"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator), BotPermission(GuildPermission.ManageWebhooks),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task SetByeAsync()
        {
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            if (string.IsNullOrEmpty(guildDb.ByeMessage))
            {
                await ReplyErrorAsync("ByeMessageNotSet");
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
                await ReplyConfirmationAsync("ByeDisabled");
                
                return;
            }
            
            if (webhook != null)
                await webhook.ModifyAsync(x => x.ChannelId = Context.Channel.Id);
            else
                webhook = await CreateWebhookAsync((SocketTextChannel) Context.Channel);

            guildDb.ByeWebhookId = webhook.Id;
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("ByeEnabled", guildDb.ByeMessage);
        }

        [Command("byemessage"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task ByeMessageAsync([Remainder] string message)
        {
            if (message.Length > 1500)
            {
                await ReplyErrorAsync("ByeMessageLengthLimit", 1500);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            guildDb.ByeMessage = message;
            await DbContext.SaveChangesAsync();

            await ReplyConfirmationAsync("ByeMessageSet");
        }

        [Command("setmodlog"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
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
                await ReplyConfirmationAsync("ModLogEnabled");
            else
                await ReplyConfirmationAsync("ModLogDisabled");
        }
        
        private async Task<RestWebhook> CreateWebhookAsync(SocketTextChannel channel)
        {
            var currentUser = channel.Guild.CurrentUser;
            await using var stream = await _httpClient.GetStreamAsync(currentUser.GetRealAvatarUrl());
            return await channel.CreateWebhookAsync(currentUser.Username, stream);
        }
    }
}