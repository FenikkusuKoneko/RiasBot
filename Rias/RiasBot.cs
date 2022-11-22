using System.Globalization;
using System.Reflection;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Interaction;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Rias.Common;
using Rias.Services;

namespace Rias;

public class RiasBot : DiscordBot
{
    protected override IEnumerable<Assembly> GetModuleAssemblies() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.FullName?.StartsWith("Rias") == true);

    private readonly LocalizationService _localization;
    private readonly ConcurrentHashSetCache<object> _rateLimits = new();

    public RiasBot(IOptions<DiscordBotConfiguration> options, ILogger<RiasBot> logger, IServiceProvider services, DiscordClient client)
        : base(options, logger, services, client)
    {
        _localization = services.GetRequiredService<LocalizationService>();
    }

    protected override LocalMessageBase CreateFailureMessage(IDiscordCommandContext context)
    {
        if (context is IDiscordInteractionCommandContext)
            return new LocalInteractionMessageResponse();

        return new LocalMessage();
    }

    protected override string? FormatFailureReason(IDiscordCommandContext context, IResult result)
    {
        switch (result)
        {
            case CommandNotFoundResult:
                return null;
            case CommandRateLimitedResult rateLimitedResult:
            {
                var (rateLimitAttribute, retryAfter) = rateLimitedResult.RateLimits.First();
                var key = GetRateLimitBucketKey(context, (RateLimitBucketType) rateLimitAttribute.BucketType);
            
                if (_rateLimits.Contains(key))
                    return null;
            
                _rateLimits.AddOrUpdate(key, retryAfter);

                return _localization.GetText(context.GuildId, Strings.Service.CommandCooldown,
                    retryAfter.Humanize(culture: new CultureInfo(_localization.GetGuildLocale(context.GuildId)), minUnit: TimeUnit.Second));
            }
            default:
                return null;
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
}