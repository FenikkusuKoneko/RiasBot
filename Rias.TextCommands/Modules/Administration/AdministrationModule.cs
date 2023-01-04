using System.Text;
using Disqord;
using Disqord.Bot.Commands;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Services.Attributes;
using Rias.Services.Commands;
using Rias.Services.Extensions;

namespace Rias.TextCommands.Modules.Administration;

[Name("Administration")]
public partial class AdministrationModule : RiasTextGuildModule<AdministrationService>
{
    private const int GreetByeMessageLengthLimit = 1500;
    
    [TextCommand("greet", "setgreet")]
    [AuthorPermissions(Permissions.Administrator)]
    [BotPermissions(Permissions.ManageWebhooks)]
    [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
    public async Task<IResult> SetGreetAsync(IMessageGuildChannel? channel = null)
    {
        var switchGreet = channel is null;
        var guild = Context.GetGuild();
        channel ??= Context.GetChannel();

        var setGreetResponse = await Service.SetGreetAsync(Context.GetCurrentMember(), guild, channel, Context.Author, switchGreet);
        
        if (!setGreetResponse.IsGreetEnabled)
            return ReplySuccessResponse(Strings.Administration.GreetDisabled);

        var message = new LocalMessage()
            .WithContent(channel.Id == Context.ChannelId
                ? GetText(Strings.Administration.GreetEnabled)
                : GetText(Strings.Administration.GreetEnabledChannel, channel.Mention));

        if (!string.IsNullOrWhiteSpace(setGreetResponse.Content))
            message.Content += $"\n\n{setGreetResponse.Content}";
        
        if (setGreetResponse.Embed is not null)
            message.AddEmbed(setGreetResponse.Embed);
        
        if (string.IsNullOrWhiteSpace(setGreetResponse.Content) && setGreetResponse.Embed is null)
        {
            var embed = new LocalEmbed()
                .WithColor(Utils.SuccessColor)
                .WithThumbnailUrl(Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 2048))
                .WithDescription(GetText(Strings.Administration.DefaultGreetMessage, Markdown.Bold(guild.Name), Markdown.Bold(Context.Author.Tag), guild.MemberCount));
            
            message.AddEmbed(embed);
        }

        return Reply(message);
    }

    [TextCommand("greetmessage", "greetmsg")]
    [AuthorPermissions(Permissions.Administrator)]
    [BotPermissions(Permissions.ManageWebhooks)]
    public async Task<IResult> GreetMessageAsync([Remainder] string? message = null)
    {
        var guild = Context.GetGuild();

        if (message is { Length: > GreetByeMessageLengthLimit })
            return ReplyErrorResponse(Strings.Administration.GreetMessageLengthLimit, GreetByeMessageLengthLimit);

        var greetMessage = Helpers.FormatPlaceholders(Context.Author, message);
        var messageParsed = Helpers.TryParseMessage(greetMessage, out var greetContent, out var greetEmbed);

        if (messageParsed && string.IsNullOrEmpty(greetContent) && greetEmbed is null)
            return ReplyErrorResponse(Strings.Administration.InvalidCustomMessage);

        var greetMessageResponse = await Service.SetGreetMessageAsync(guild, message);
        var content = new StringBuilder()
            .AppendLine(string.IsNullOrEmpty(message)
                ? GetText(Strings.Administration.GreetMessageSetToDefault)
                : GetText(Strings.Administration.GreetMessageSet));

        if (!greetMessageResponse.IsGreetEnabled || greetMessageResponse.Channel is null)
        {
            content.AppendLine(GetText(Strings.Administration.GreetDisabled));
        }
        else
        {
            content.AppendLine(greetMessageResponse.Channel.Id == Context.ChannelId
                ? GetText(Strings.Administration.GreetEnabled)
                : GetText(Strings.Administration.GreetEnabledChannel, greetMessageResponse.Channel.Mention));
        }
        
        if (!messageParsed)
            content.AppendLine().AppendLine(greetMessage);
        else if (!string.IsNullOrWhiteSpace(greetContent))
            content.AppendLine().AppendLine(greetContent);
        
        var replyMessage = new LocalMessage().WithContent(content.ToString());

        if (greetEmbed is not null)
        {
            replyMessage.AddEmbed(greetEmbed);
        }
        else if (string.IsNullOrEmpty(message))
        {
            var defaultEmbed = new LocalEmbed()
                .WithColor(Utils.SuccessColor)
                .WithThumbnailUrl(Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 2048))
                .WithDescription(GetText(Strings.Administration.DefaultGreetMessage, Markdown.Bold(guild.Name), Markdown.Bold(Context.Author.Tag), guild.MemberCount));
            
            replyMessage.AddEmbed(defaultEmbed);
        }
        
        return Reply(replyMessage);
    }
    
    [TextCommand("bye", "setbye")]
    [AuthorPermissions(Permissions.Administrator)]
    [BotPermissions(Permissions.ManageWebhooks)]
    [RateLimit(1, 5, RateLimitMeasure.Seconds, RateLimitBucketType.Guild)]
    public async Task<IResult> SetByeAsync(IMessageGuildChannel? channel = null)
    {
        var switchBye = channel is null;
        var guild = Context.GetGuild();
        channel ??= Context.GetChannel();

        var setByeResponse = await Service.SetByeAsync(Context.GetCurrentMember(), guild, channel, Context.Author, switchBye);
        
        if (!setByeResponse.IsByeEnabled)
            return ReplySuccessResponse(Strings.Administration.ByeDisabled);

        var message = new LocalMessage()
            .WithContent(channel.Id == Context.ChannelId
                ? GetText(Strings.Administration.ByeEnabled)
                : GetText(Strings.Administration.ByeEnabledChannel, channel.Mention));

        if (!string.IsNullOrWhiteSpace(setByeResponse.Content))
            message.Content += $"\n\n{setByeResponse.Content}";
        
        if (setByeResponse.Embed is not null)
            message.AddEmbed(setByeResponse.Embed);
        
        if (string.IsNullOrWhiteSpace(setByeResponse.Content) && setByeResponse.Embed is null)
        {
            var embed = new LocalEmbed()
                .WithColor(Utils.SuccessColor)
                .WithThumbnailUrl(Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 2048))
                .WithDescription(GetText(Strings.Administration.DefaultByeMessage, Markdown.Bold(Context.Author.Tag), guild.MemberCount));
            
            message.AddEmbed(embed);
        }

        return Reply(message);
    }
    
    [TextCommand("byemessage", "byemsg")]
    [AuthorPermissions(Permissions.Administrator)]
    [BotPermissions(Permissions.ManageWebhooks)]
    public async Task<IResult> ByeMessageAsync([Remainder] string? message = null)
    {
        var guild = Context.GetGuild();

        if (message is { Length: > GreetByeMessageLengthLimit })
            return ReplyErrorResponse(Strings.Administration.ByeMessageLengthLimit, GreetByeMessageLengthLimit);

        var byeMessage = Helpers.FormatPlaceholders(Context.Author, message);
        var messageParsed = Helpers.TryParseMessage(byeMessage, out var byeContent, out var byeEmbed);

        if (messageParsed && string.IsNullOrEmpty(byeContent) && byeEmbed is null)
            return ReplyErrorResponse(Strings.Administration.InvalidCustomMessage);

        var byeMessageResponse = await Service.SetByeMessageAsync(guild, message);
        var content = new StringBuilder()
            .AppendLine(string.IsNullOrEmpty(message)
                ? GetText(Strings.Administration.ByeMessageSetToDefault)
                : GetText(Strings.Administration.ByeMessageSet));

        if (!byeMessageResponse.IsByeEnabled || byeMessageResponse.Channel is null)
        {
            content.AppendLine(GetText(Strings.Administration.ByeDisabled));
        }
        else
        {
            content.AppendLine(byeMessageResponse.Channel.Id == Context.ChannelId
                ? GetText(Strings.Administration.ByeEnabled)
                : GetText(Strings.Administration.ByeEnabledChannel, byeMessageResponse.Channel.Mention));
        }
        
        if (!messageParsed)
            content.AppendLine().AppendLine(byeMessage);
        else if (!string.IsNullOrWhiteSpace(byeContent))
            content.AppendLine().AppendLine(byeContent);
        
        var replyMessage = new LocalMessage().WithContent(content.ToString());

        if (byeEmbed is not null)
        {
            replyMessage.AddEmbed(byeEmbed);
        }
        else if (string.IsNullOrEmpty(message))
        {
            var defaultEmbed = new LocalEmbed()
                .WithColor(Utils.SuccessColor)
                .WithThumbnailUrl(Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 2048))
                .WithDescription(GetText(Strings.Administration.DefaultByeMessage, Markdown.Bold(Context.Author.Tag), guild.MemberCount));
            
            replyMessage.AddEmbed(defaultEmbed);
        }
        
        return Reply(replyMessage);
    }

    [TextCommand("setmodlog", "modlog")]
    [AuthorPermissions(Permissions.Administrator)]
    public async Task<IResult> SetModLogAsync(IMessageGuildChannel? channel = null)
    {
        var toggleModLog = channel is null;
        channel ??= Context.GetChannel();
        
        var modLogEnabled = await Service.SetModLogAsync(channel, toggleModLog);

        if (modLogEnabled)
        {
            return channel.Id == Context.ChannelId
                ? ReplySuccessResponse(Strings.Administration.ModLogEnabled)
                : ReplySuccessResponse(Strings.Administration.ModLogEnabledChannel, channel.Mention);
        }

        return ReplySuccessResponse(Strings.Administration.ModLogDisabled);
    }
}