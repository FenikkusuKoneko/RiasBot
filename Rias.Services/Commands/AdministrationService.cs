using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Rias.Common;
using Rias.Database;
using Rias.Database.Entities;
using Rias.Services.Extensions;
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
        bool toggleGreet)
    {
        var guildEntity = await Db.Guilds.GetOrAddAsync(
            g => g.GuildId == guild.Id,
            () => new GuildEntity { GuildId = guild.Id });

        if (toggleGreet)
            guildEntity.IsGreetEnabled = !guildEntity.IsGreetEnabled;
        else
            guildEntity.IsGreetEnabled = true;
        
        var webhook = guildEntity.GreetWebhookId > 0
            ? await guild.GetWebhookAsync(guildEntity.GreetWebhookId)
            : null;
        
        if (!guildEntity.IsGreetEnabled)
        {
            if (webhook is not null)
                await webhook.DeleteAsync();

            guildEntity.GreetWebhookId = 0;
            Db.Guilds.Update(guildEntity);
            await Db.SaveChangesAsync();

            return new SetGreetResponse
            {
                IsGreetEnabled = false
            };
        }
        
        await using var stream = await _httpClient.GetStreamAsync(currentUser.GetAvatarUrl(CdnAssetFormat.Automatic, 2048));
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
                IsGreetEnabled = true,
                Content = content,
                Embed = embed
            };
        }

        return new SetGreetResponse
        {
            IsGreetEnabled = true,
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
            ? await guild.GetWebhookAsync(guildEntity.GreetWebhookId)
            : null;

        var channel = webhook?.ChannelId is not null
            ? guild.GetChannel(webhook.ChannelId.Value)
            : null;

        return new SetGreetMessageResponse
        {
            IsGreetEnabled = guildEntity.IsGreetEnabled,
            Channel = channel
        };
    }
    
    public async Task<SetByeResponse> SetByeAsync(
        IMember currentUser,
        IGuild guild,
        IMessageGuildChannel channel,
        IMember member,
        bool toggleBye)
    {
        var guildEntity = await Db.Guilds.GetOrAddAsync(
            g => g.GuildId == guild.Id,
            () => new GuildEntity { GuildId = guild.Id });

        if (toggleBye)
            guildEntity.IsByeEnabled = !guildEntity.IsByeEnabled;
        else
            guildEntity.IsByeEnabled = true;
        
        var webhook = guildEntity.ByeWebhookId > 0
            ? await guild.GetWebhookAsync(guildEntity.ByeWebhookId)
            : null;
        
        if (!guildEntity.IsByeEnabled)
        {
            if (webhook is not null)
                await webhook.DeleteAsync();

            guildEntity.ByeWebhookId = 0;
            Db.Guilds.Update(guildEntity);
            await Db.SaveChangesAsync();

            return new SetByeResponse
            {
                IsByeEnabled = false
            };
        }
        
        await using var stream = await _httpClient.GetStreamAsync(currentUser.GetAvatarUrl(CdnAssetFormat.Automatic, 2048));
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
                IsByeEnabled = true,
                Content = content,
                Embed = embed
            };
        }

        return new SetByeResponse
        {
            IsByeEnabled = true,
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
            ? await guild.GetWebhookAsync(guildEntity.ByeWebhookId)
            : null;

        var channel = webhook?.ChannelId is not null
            ? guild.GetChannel(webhook.ChannelId.Value)
            : null;

        return new SetByeMessageResponse
        {
            IsByeEnabled = guildEntity.IsByeEnabled,
            Channel = channel
        };
    }

    public async Task<bool> SetModLogAsync(IMessageGuildChannel channel, bool toggleModLog)
    {
        var guildEntity = await Db.Guilds.GetOrAddAsync(
            g => g.GuildId == channel.GuildId, 
            () => new GuildEntity { GuildId = channel.GuildId });

        if (toggleModLog)
        {
            guildEntity.ModLogChannelId = guildEntity.ModLogChannelId == 0
                ? channel.Id
                : 0;
        }
        else
        {
            guildEntity.ModLogChannelId = channel.Id;
        }

        Db.Guilds.Update(guildEntity);
        await Db.SaveChangesAsync();
        
        return guildEntity.ModLogChannelId > 0;
    }
}