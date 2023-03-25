using System.Diagnostics;
using Disqord.Bot.Commands.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rias.Database;
using Rias.Services.Providers;

namespace Rias.Services.Hosted;

public class PrefixesBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RiasPrefixProvider _prefixProvider;
    private readonly ILogger<PrefixesBackgroundService> _logger;

    public PrefixesBackgroundService(
        IServiceProvider serviceProvider,
        IPrefixProvider prefixProvider,
        ILogger<PrefixesBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _prefixProvider = (RiasPrefixProvider) prefixProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();
        
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        var prefixes = await db.Guilds.AsNoTracking().Where(g => !string.IsNullOrEmpty(g.Prefix)).ToListAsync(stoppingToken);
        
        foreach (var prefix in prefixes)
            _prefixProvider.AddOrUpdatePrefix(prefix.GuildId, prefix.Prefix ?? string.Empty);
        
        sw.Stop();
        _logger.LogInformation("Loaded {Count} guild prefixes in {Elapsed}ms", prefixes.Count, sw.ElapsedMilliseconds);
    }
}