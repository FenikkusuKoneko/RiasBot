using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Rias.Common;
using Rias.Database;
using Rias.Database.Entities;
using Rias.Services.Responses.Administration;

namespace Rias.Services.Commands;

public class AdministrationService : RiasCommandService
{
    private readonly HttpClient _httpClient;
    
    public AdministrationService(HttpClient httpClient, RiasDbContext db)
        : base(db)
    {
        _httpClient = httpClient;
    }

    public async Task<SetGreetResponse> SetGreetAsync(
        IMember currentUser,
        IGuild guild,
        IMessageGuildChannel channel,
        IMember member,
        bool switchGreet)
    {
        var guildEntity = await Db.Guilds.GetOrAddAsync(
            g => g.GuildId == guild.Id,
            () => new GuildEntity { GuildId = guild.Id });

        if (switchGreet)
            guildEntity.GreetNotification = !guildEntity.GreetNotification;
        else
            guildEntity.GreetNotification = true;
        
        var webhook = guildEntity.GreetWebhookId > 0
            ? (await guild.FetchWebhooksAsync()).FirstOrDefault(w => w.Id == guildEntity.GreetWebhookId)
            : null;
        
        if (!guildEntity.GreetNotification)
        {
            if (webhook is not null)
                await webhook.DeleteAsync();

            guildEntity.GreetWebhookId = 0;
            Db.Guilds.Update(guildEntity);
            await Db.SaveChangesAsync();

            return new SetGreetResponse
            {
                GreetEnabled = false
            };
        }
        
        await using var stream = await _httpClient.GetStreamAsync(currentUser.GetAvatarUrl());
        await using var webhookAvatar = new MemoryStream();
        await stream.CopyToAsync(webhookAvatar);
        webhookAvatar.Position = 0;
        
        if (webhook is null)
        {
            webhook = await channel.CreateWebhookAsync(currentUser.Name, props => props.Avatar = webhookAvatar);
        }
        else
        {
            await webhook.ModifyAsync(props =>
            {
                props.Name = currentUser.Name;
                props.Avatar = webhookAvatar;
                props.ChannelId = channel.Id;
            });
        }
        
        guildEntity.GreetWebhookId = webhook.Id;
        Db.Guilds.Update(guildEntity);
        await Db.SaveChangesAsync();

        var greetMessage = Helpers.FormatPlaceholders(member, guildEntity.GreetMessage);
        
        if (Helpers.TryParseMessage(greetMessage, out var content, out var embed))
        {
            return new SetGreetResponse
            {
                GreetEnabled = true,
                Content = content,
                Embed = embed
            };
        }

        return new SetGreetResponse
        {
            GreetEnabled = true,
            Content = greetMessage
        };
    }

    public async Task<SetGreetMessageResponse> SetGreetMessageAsync(IGuild guild, string? message)
    {
        var guildEntity = await Db.Guilds.GetOrAddAsync(
            g => g.GuildId == guild.Id, 
            () => new GuildEntity { GuildId = guild.Id });

        guildEntity.GreetMessage = message;
        Db.Guilds.Update(guildEntity);
        await Db.SaveChangesAsync();

        var webhook = guildEntity.GreetWebhookId > 0
            ? (await guild.FetchWebhooksAsync()).FirstOrDefault(w => w.Id == guildEntity.GreetWebhookId)
            : null;

        var channel = webhook?.ChannelId is not null
            ? guild.GetChannel(webhook.ChannelId.Value)
            : null;

        return new SetGreetMessageResponse
        {
            GreetEnabled = guildEntity.GreetNotification,
            Channel = channel
        };
    }
    
    public async Task<SetByeResponse> SetByeAsync(
        IMember currentUser,
        IGuild guild,
        IMessageGuildChannel channel,
        IMember member,
        bool switchBye)
    {
        var guildEntity = await Db.Guilds.GetOrAddAsync(
            g => g.GuildId == guild.Id,
            () => new GuildEntity { GuildId = guild.Id });

        if (switchBye)
            guildEntity.ByeNotification = !guildEntity.ByeNotification;
        else
            guildEntity.ByeNotification = true;
        
        var webhook = guildEntity.ByeWebhookId > 0
            ? (await guild.FetchWebhooksAsync()).FirstOrDefault(w => w.Id == guildEntity.ByeWebhookId)
            : null;
        
        if (!guildEntity.ByeNotification)
        {
            if (webhook is not null)
                await webhook.DeleteAsync();

            guildEntity.ByeWebhookId = 0;
            Db.Guilds.Update(guildEntity);
            await Db.SaveChangesAsync();

            return new SetByeResponse
            {
                ByeEnabled = false
            };
        }
        
        await using var stream = await _httpClient.GetStreamAsync(currentUser.GetAvatarUrl());
        await using var webhookAvatar = new MemoryStream();
        await stream.CopyToAsync(webhookAvatar);
        webhookAvatar.Position = 0;
        
        if (webhook is null)
        {
            webhook = await channel.CreateWebhookAsync(currentUser.Name, props => props.Avatar = webhookAvatar);
        }
        else
        {
            await webhook.ModifyAsync(props =>
            {
                props.Name = currentUser.Name;
                props.Avatar = webhookAvatar;
                props.ChannelId = channel.Id;
            });
        }
        
        guildEntity.ByeWebhookId = webhook.Id;
        Db.Guilds.Update(guildEntity);
        await Db.SaveChangesAsync();

        var byeMessage = Helpers.FormatPlaceholders(member, guildEntity.ByeMessage);
        
        if (Helpers.TryParseMessage(byeMessage, out var content, out var embed))
        {
            return new SetByeResponse
            {
                ByeEnabled = true,
                Content = content,
                Embed = embed
            };
        }

        return new SetByeResponse
        {
            ByeEnabled = true,
            Content = byeMessage
        };
    }
    
    public async Task<SetByeMessageResponse> SetByeMessageAsync(IGuild guild, string? message)
    {
        var guildEntity = await Db.Guilds.GetOrAddAsync(
            g => g.GuildId == guild.Id, 
            () => new GuildEntity { GuildId = guild.Id });

        guildEntity.ByeMessage = message;
        Db.Guilds.Update(guildEntity);
        await Db.SaveChangesAsync();

        var webhook = guildEntity.ByeWebhookId > 0
            ? (await guild.FetchWebhooksAsync()).FirstOrDefault(w => w.Id == guildEntity.ByeWebhookId)
            : null;

        var channel = webhook?.ChannelId is not null
            ? guild.GetChannel(webhook.ChannelId.Value)
            : null;

        return new SetByeMessageResponse
        {
            ByeEnabled = guildEntity.ByeNotification,
            Channel = channel
        };
    }
}