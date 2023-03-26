using System.Diagnostics;
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

namespace Rias;

public class RiasBot : DiscordBot, IRiasBot
{
    private readonly string _version = "4.0.0-beta";
    private readonly string _author = "Koneko#0001";
    private readonly Snowflake _authorId = 327927038360944640;
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    private readonly LocalisationService _localisation;
    private readonly RiasConfiguration _configuration;

    private readonly ConcurrentHashSetCache<object> _rateLimits = new();
    private readonly ConcurrentHashSet<string> _exceptions = new();

    private CachedMessageGuildChannel? _logsChannel;

    public RiasBot(
        IOptions<RiasConfiguration> riasOptions,
        IOptions<DiscordBotConfiguration> options,
        ILogger<RiasBot> logger,
        IServiceProvider services,
        DiscordClient client)
        : base(options, logger, services, client)
    {
        _localisation = services.GetRequiredService<LocalisationService>();
        _configuration = riasOptions.Value;

        Ready += OnReadyAsync;
    }

    public string Version => _version;
    public string Author => _author;
    public Snowflake AuthorId => _authorId;
    public TimeSpan ElapsedTime => _uptime.Elapsed;
    
    protected override IEnumerable<Assembly> GetModuleAssemblies() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.FullName?.StartsWith("Rias") == true);

    protected override ValueTask AddTypeParsers(DefaultTypeParserProvider typeParserProvider, CancellationToken cancellationToken)
    {
        typeParserProvider.AddParser(new Services.TypeParsers.MemberTypeParser());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<IGuildChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<ICategorizableGuildChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<IMessageGuildChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<IVocalGuildChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<ITextChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<IVoiceChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<ICategoryChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<IStageChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<IThreadChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildChannelTypeParser<IForumChannel>());
        typeParserProvider.AddParser(new Services.TypeParsers.CustomEmojiTypeParser());
        typeParserProvider.AddParser(new Services.TypeParsers.GuildEmojiTypeParser());

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
                    return string.Join('\n', checksFailedResult.FailedChecks.Select(check =>
                        check.Key is RequireGuildAttribute ? "• This command can be executed only in a server." : $"• {check.Value.FailureReason}"));
                case TypeParseFailedResult typeParseFailedResult:
                    return $"• {typeParseFailedResult.FailureReason}";
                case ParameterChecksFailedResult parameterChecksFailedResult:
                    return string.Join('\n', parameterChecksFailedResult.FailedChecks.Select(check => $"• {check.Value.FailureReason}"));
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
            .WithDescription(reason);

        if (result is OverloadsFailedResult overloadsFailedResult)
        {
            var textCommand = overloadsFailedResult.FailedOverloads.First().Key;
            PopulateTextCommandEmbed(textCommand);
        }
        else
        {
            switch (context.Command)
            {
                case TextCommand textCommand:
                    PopulateTextCommandEmbed(textCommand);
                    break;
                case ApplicationCommand applicationCommand:
                    embed.WithAuthor(applicationCommand.Alias);
                    break;
            }
        }

        void PopulateTextCommandEmbed(ITextCommand textCommand1)
        {
            var moduleAlias = textCommand1.Module.Aliases.Count != 0 ? $"{textCommand1.Module.Aliases[0]} " : string.Empty;
            var title = string.Join(" / ", textCommand1.Aliases.Select(a => $"{moduleAlias}{a}"));
            embed.WithTitle(title);

            var textCommandContext = (IDiscordTextCommandContext) context;
            var footer = _localisation.GetText(context.GuildId, Strings.Service.CommandNotExecutedFooter,
                context.Author.Tag, textCommandContext.Prefix.Stringify(), textCommand1.Name);

            embed.WithFooter(footer, context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 128));
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

    private Task OnReadyAsync(object? sender, ReadyEventArgs args)
    {
        if (CacheProvider.TryGetChannels(_configuration.LogsServerId, out var channels)
            && channels.TryGetValue(_configuration.LogsChannelId, out var channel))
            _logsChannel = channel as CachedMessageGuildChannel;

        return Task.CompletedTask;
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