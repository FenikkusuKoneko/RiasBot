using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ConcurrentCollections;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Text;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Qmmands.Default;
using Qmmands.Text;
using Rias.Common;
using Rias.Services;
using Rias.Services.TypeParsers;

namespace Rias;

public class RiasBot : DiscordBot
{
    protected override IEnumerable<Assembly> GetModuleAssemblies() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.FullName?.StartsWith("Rias") == true);

    private readonly LocalisationService _localisation;
    private readonly RiasOptions _riasOptions;
    
    private readonly ConcurrentHashSetCache<object> _rateLimits = new();
    private readonly ConcurrentHashSet<string> _exceptions = new();

    private CachedMessageGuildChannel? _logsChannel;

    public RiasBot(
        IOptions<RiasOptions> riasOptions,
        IOptions<DiscordBotConfiguration> options,
        ILogger<RiasBot> logger,
        IServiceProvider services,
        DiscordClient client)
        : base(options, logger, services, client)
    {
        _localisation = services.GetRequiredService<LocalisationService>();
        _riasOptions = riasOptions.Value;
        
        Ready += OnReadyAsync;
    }

    protected override ValueTask AddTypeParsers(DefaultTypeParserProvider typeParserProvider, CancellationToken cancellationToken)
    {
        // typeParserProvider.AddParser(new Disqord.Bot.Commands.Parsers.SnowflakeTypeParser());
        // typeParserProvider.AddParser(new Disqord.Bot.Commands.Parsers.ColorTypeParser());
        // typeParserProvider.AddParser(new Disqord.Bot.Commands.Parsers.CustomEmojiTypeParser());
        // typeParserProvider.AddParser(new Disqord.Bot.Commands.Parsers.GuildEmojiTypeParser());
        typeParserProvider.AddParser(new GuildChannelTypeParser<IGuildChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<ICategorizableGuildChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<IMessageGuildChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<IVocalGuildChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<ITextChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<IVoiceChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<ICategoryChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<IStageChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<IThreadChannel>());
        typeParserProvider.AddParser(new GuildChannelTypeParser<IForumChannel>());
        // typeParserProvider.AddParser(new Disqord.Bot.Commands.Parsers.MemberTypeParser());
        // typeParserProvider.AddParser(new Disqord.Bot.Commands.Parsers.RoleTypeParser());
        return default;
    }

    protected override string? FormatFailureReason(IDiscordCommandContext context, IResult result)
    {
        if (result is OverloadsFailedResult overloadsFailedResult)
            return string.Join('\n', overloadsFailedResult.FailedOverloads
                .Select(overload => GetFailureReason(overload.Value)).ToHashSet());

        return GetFailureReason(result);

        string? GetFailureReason(IResult innerResult)
        {
            switch (innerResult)
            {
                case CommandNotFoundResult:
                    return null;
                case ChecksFailedResult checksFailedResult:
                    return string.Join('\n', checksFailedResult.FailedChecks.Select(check => $"• {check.Value.FailureReason}"));
                case TypeParseFailedResult typeParseFailedResult:
                    return typeParseFailedResult.FailureReason;
                case CommandRateLimitedResult rateLimitedResult:
                {
                    var (rateLimitAttribute, retryAfter) = rateLimitedResult.RateLimits.First();
                    var key = GetRateLimitBucketKey(context, (RateLimitBucketType) rateLimitAttribute.BucketType);
            
                    if (_rateLimits.Contains(key))
                        return null;
            
                    _rateLimits.AddOrUpdate(key, retryAfter);

                    return _localisation.GetText(context.GuildId, Strings.Service.CommandCooldown,
                        retryAfter.Humanize(culture: new CultureInfo(_localisation.GetGuildLocale(context.GuildId)), minUnit: TimeUnit.Second));
                }
                case ExceptionResult exceptionResult:
                    _ = SendExceptionLogAsync(context, exceptionResult.Exception);
                    return _localisation.GetText(context.GuildId, Strings.Service.CommandException);
                default:
                    Logger.LogError("[{ResultType}] - {FailureReason}", innerResult.GetType().Name, innerResult.FailureReason);
                    return null;
            }
        }
    }

    protected override bool FormatFailureMessage(IDiscordCommandContext context, LocalMessageBase message, IResult result)
    {
        var reason = FormatFailureReason(context, result);
        if (reason is null)
            return false;
        
        var embed = new LocalEmbed()
            .WithColor(Utils.ErrorColor)
            .WithTitle(_localisation.GetText(context.GuildId, Strings.Service.CommandNotExecuted))
            .WithDescription(reason);

        switch (context.Command)
        {
            case TextCommand textCommand:
            {
                var moduleAlias = textCommand.Module.Aliases.Count != 0 ? $"{textCommand.Module.Aliases[0]} " : string.Empty;
                var author = string.Join(" / ", textCommand.Aliases.Select(a => $"{moduleAlias}{a}"));
                embed.WithAuthor(author);

                var textCommandContext = (IDiscordTextCommandContext) context;
                var footer = _localisation.GetText(context.GuildId, Strings.Service.CommandNotExecutedFooter,
                    context.Author.Tag, textCommandContext.Prefix.Stringify(), textCommand.Name);

                embed.WithFooter(footer, context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 128));
                break;
            }
            case ApplicationCommand applicationCommand:
                embed.WithAuthor(applicationCommand.Alias);
                break;
        }

        message.AddEmbed(embed);
        message.WithAllowedMentions(LocalAllowedMentions.None);
        return true;
    }

    protected override object GetRateLimitBucketKey(IDiscordCommandContext context, RateLimitBucketType bucketType)
    {
        return IsOwnerAsync(context.AuthorId).AsTask().GetAwaiter().GetResult()
            ? null!
            : base.GetRateLimitBucketKey(context, bucketType);
    }

    private ValueTask OnReadyAsync(object? sender, ReadyEventArgs? args)
    {
        if (CacheProvider.TryGetChannels(_riasOptions.LogsServerId, out var channels)
            && channels.TryGetValue(_riasOptions.LogsChannelId, out var channel))
            _logsChannel = channel as CachedMessageGuildChannel;

        return default;
    }

    private async Task SendExceptionLogAsync(IDiscordCommandContext context, Exception exception)
    {
        try
        {
            var exceptionString = exception.ToString();
            var exceptionBytes = Encoding.UTF8.GetBytes(exceptionString);
            
            var exceptionHashBytes = MD5.HashData(exceptionBytes);
            var exceptionHash = Convert.ToHexString(exceptionHashBytes);
            
            if (_logsChannel is not null && !_exceptions.Contains(exceptionHash))
            {
                var message = new LocalMessage();
                
                var embed = new LocalEmbed()
                    .WithColor(Utils.ErrorColor)
                    .WithAuthor(context.Author)
                    .WithTitle($"Command {context.Command?.Name} threw an exception");

                if (exceptionString.Length < Discord.Limits.Message.Embed.MaxDescriptionLength)
                {
                    embed.WithDescription(exceptionString);
                }
                else
                {
                    embed.WithDescription("See the attached file for the full exception.");

                    using var stream = new MemoryStream(exceptionBytes);
                    using var attachment = new LocalAttachment(stream, $"{DateTime.UtcNow}.txt");
                    message.AddAttachment(attachment);
                }

                message.AddEmbed(embed);
                await _logsChannel.SendMessageAsync(message);
                _exceptions.Add(exceptionHash);
            }
        }
        catch (Exception ex)
        {
            _logsChannel = null;
            Logger.LogError(ex, "Failed to send exception log");
        }
    }
}