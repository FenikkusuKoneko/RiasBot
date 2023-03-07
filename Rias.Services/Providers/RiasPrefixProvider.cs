using System.Collections.Concurrent;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Text;
using Disqord.Gateway;
using Microsoft.Extensions.Options;
using Rias.Common;

namespace Rias.Services.Providers;

public class RiasPrefixProvider : IPrefixProvider
{
    private readonly RiasConfiguration _configuration;

    private readonly ConcurrentDictionary<ulong, string> _guildPrefixes = new();

    public RiasPrefixProvider(IOptions<RiasConfiguration> options)
    {
        _configuration = options.Value;
    }
    
    public ValueTask<IEnumerable<IPrefix>?> GetPrefixesAsync(IGatewayUserMessage message)
    {
        var prefixes = new HashSet<IPrefix>();

        if (_configuration.Prefixes is not null)
        {
            foreach (var prefix in _configuration.Prefixes)
                prefixes.Add(new StringPrefix(prefix));
        }

        if (message.GuildId.HasValue && _guildPrefixes.TryGetValue(message.GuildId.Value, out var guildPrefix))
            prefixes.Add(new StringPrefix(guildPrefix));

        prefixes.Add(new StringPrefix(message.Client.CurrentUser.Name));
        prefixes.Add(new MentionPrefix(message.Client.CurrentUser.Id));

        return new ValueTask<IEnumerable<IPrefix>?>(prefixes);
    }

    public string? GetPrefix(Snowflake guildId)
    {
        return _guildPrefixes.TryGetValue(guildId, out var prefix)
            ? prefix 
            : _configuration.Prefixes?.FirstOrDefault() ?? null;
    }

    public void AddOrUpdatePrefix(Snowflake guildId, string prefix)
        => _guildPrefixes[guildId] = prefix;

    public void RemovePrefix(Snowflake guildId)
        => _guildPrefixes.TryRemove(guildId, out _);
}