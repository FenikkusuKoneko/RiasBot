using System.Collections.Concurrent;
using System.Diagnostics;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Text;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rias.Common;
using Rias.Database;

namespace Rias.Services;

public class RiasPrefixProvider : IPrefixProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RiasOptions _options;
    private readonly ILogger<RiasPrefixProvider> _logger;

    private readonly ConcurrentDictionary<ulong, string> _guildPrefixes = new();

    public RiasPrefixProvider(IServiceProvider serviceProvider, IOptions<RiasOptions> options, ILogger<RiasPrefixProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }
    
    public ValueTask<IEnumerable<IPrefix>?> GetPrefixesAsync(IGatewayUserMessage message)
    {
        var prefixes = new HashSet<IPrefix>();

        if (_options.Prefixes is not null)
        {
            foreach (var prefix in _options.Prefixes)
                prefixes.Add(new StringPrefix(prefix));
        }

        if (message.GuildId.HasValue && _guildPrefixes.TryGetValue(message.GuildId.Value, out var guildPrefix))
            prefixes.Add(new StringPrefix(guildPrefix));

        prefixes.Add(new StringPrefix(message.Client.CurrentUser.Name));
        prefixes.Add(new MentionPrefix(message.Client.CurrentUser.Id));

        return new ValueTask<IEnumerable<IPrefix>?>(prefixes);
    }
    
    public async Task LoadGuildPrefixesAsync()
    {
        var sw = Stopwatch.StartNew();
        
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        var prefixes = await db.Guilds.Where(g => !string.IsNullOrEmpty(g.Prefix)).ToListAsync();
        
        foreach (var prefix in prefixes)
            _guildPrefixes.TryAdd(prefix.GuildId, prefix.Prefix ?? string.Empty);
        
        sw.Stop();
        _logger.LogInformation("Loaded {Count} guild prefixes in {Elapsed}ms", prefixes.Count, sw.ElapsedMilliseconds);
    }

    public string? GetPrefix(Snowflake guildId)
    {
        return _guildPrefixes.TryGetValue(guildId, out var prefix)
            ? prefix 
            : _options.Prefixes?.FirstOrDefault() ?? null;
    }
    
    public void SetPrefix(Snowflake guildId, string? prefix)
    {
        if (prefix is null)
            _guildPrefixes.TryRemove(guildId, out _);
        else
            _guildPrefixes.AddOrUpdate(guildId, prefix, (_, _) => prefix);
    }
}