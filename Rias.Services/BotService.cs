using System.Collections.Concurrent;
using Disqord;
using Disqord.Gateway;
using Disqord.Hosting;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Common;
using Rias.Database;
using Rias.Services.Extensions;

namespace Rias.Services;

public class BotService : DiscordClientService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LocalisationService _localisation;
    
    private readonly ConcurrentDictionary<Snowflake, List<IWebhook>> _webhooks = new();

    public BotService(IServiceProvider serviceProvider, LocalisationService localisation)
    {
        _serviceProvider = serviceProvider;
        _localisation = localisation;
    }
    
    protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs args)
    {
        if (!args.Member.IsPending && args.Guild is not null)
            await SendGreetMessageAsync(args.Guild, args.Member);
    }

    protected override async ValueTask OnMemberLeft(MemberLeftEventArgs args)
    {
        if (args.Guild is not null)
            await SendByeMessageAsync(args.Guild, args.User);
    }

    private async Task SendGreetMessageAsync(CachedGuild guild, IUser member)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        var guildEntity = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == guild.Id);

        if (guildEntity is null || !guildEntity.IsGreetEnabled)
            return;

        var currentMember = guild.GetCurrentMember();

        if ((currentMember.CalculateGuildPermissions() & Permissions.ManageWebhooks) == 0)
        {
            await DisableGreetAsync();
            return;
        }
        
        var webhooks = _webhooks.GetOrAdd(guild.Id, _ => new List<IWebhook>());
        var webhook = webhooks.FirstOrDefault(x => x.Id == guildEntity.GreetWebhookId);

        if (webhook is null)
        {
            webhook = guildEntity.GreetWebhookId > 0
                ? await guild.GetWebhookAsync(guildEntity.GreetWebhookId)
                : null;

            if (webhook is null)
            {
                await DisableGreetAsync();
                return;
            }

            webhooks.Add(webhook);
        }

        var greetMessage = Helpers.FormatPlaceholders(member, guildEntity.GreetMessage);
        var messageParsed = Helpers.TryParseMessage(greetMessage, out var content, out var embed);
        
        var message = new LocalWebhookMessage();
        
        if (!messageParsed)
            message.Content = greetMessage;
        else if (!string.IsNullOrEmpty(content))
            message.Content = content;

        if (embed is not null)
        {
            message.AddEmbed(embed);
        }
        else
        
        {
            var defaultEmbed = new LocalEmbed()
                .WithColor(Utils.ConfirmationColor)
                .WithThumbnailUrl(member.GetAvatarUrl(CdnAssetFormat.Automatic, 2048))
                .WithDescription(_localisation.GetText(guild.Id, Strings.Administration.DefaultGreetMessage, Markdown.Bold(guild.Name), Markdown.Bold(member.Tag), guild.MemberCount));

            message.AddEmbed(defaultEmbed);
        }
        
        await webhook.ExecuteAsync(message);

        async Task DisableGreetAsync()
        {
            guildEntity.IsGreetEnabled = false;
            await db.SaveChangesAsync();
        }
    }
    
    private async Task SendByeMessageAsync(CachedGuild guild, IUser user)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        var guildEntity = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == guild.Id);

        if (guildEntity is null || !guildEntity.IsByeEnabled)
            return;

        var currentMember = guild.GetCurrentMember();

        if ((currentMember.CalculateGuildPermissions() & Permissions.ManageWebhooks) == 0)
        {
            await DisableByeAsync();
            return;
        }
        
        var webhooks = _webhooks.GetOrAdd(guild.Id, _ => new List<IWebhook>());
        var webhook = webhooks.FirstOrDefault(x => x.Id == guildEntity.ByeWebhookId);

        if (webhook is null)
        {
            webhook = guildEntity.ByeWebhookId > 0
                ? await guild.GetWebhookAsync(guildEntity.ByeWebhookId)
                : null;

            if (webhook is null)
            {
                await DisableByeAsync();
                return;
            }

            webhooks.Add(webhook);
        }

        var byeMessage = Helpers.FormatPlaceholders(user, guildEntity.ByeMessage);
        var messageParsed = Helpers.TryParseMessage(byeMessage, out var content, out var embed);
        
        var message = new LocalWebhookMessage();
        
        if (!messageParsed)
            message.Content = byeMessage;
        else if (!string.IsNullOrEmpty(content))
            message.Content = content;

        if (embed is not null)
        {
            message.AddEmbed(embed);
        }
        else
        
        {
            var defaultEmbed = new LocalEmbed()
                .WithColor(Utils.ErrorColor)
                .WithThumbnailUrl(user.GetAvatarUrl(CdnAssetFormat.Automatic, 2048))
                .WithDescription(_localisation.GetText(guild.Id, Strings.Administration.DefaultByeMessage, Markdown.Bold(user.Tag), guild.MemberCount));

            message.AddEmbed(defaultEmbed);
        }
        
        await webhook.ExecuteAsync(message);

        async Task DisableByeAsync()
        {
            guildEntity.IsByeEnabled = false;
            await db.SaveChangesAsync();
        }
    }
}