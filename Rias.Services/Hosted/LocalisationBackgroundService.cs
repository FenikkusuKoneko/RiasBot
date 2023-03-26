using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rias.Database;

namespace Rias.Services.Hosted;

public class LocalisationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LocalisationBackgroundService> _logger;
    private readonly LocalisationService _localisationService;

    private readonly string _localesPath = Path.Combine(Environment.CurrentDirectory, "assets/l10n");

    public LocalisationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LocalisationBackgroundService> logger,
        LocalisationService localisationService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localisationService = localisationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Loading general locales
        var sw = Stopwatch.StartNew();
        var counter = 0;
        var valuesCounter = 0;

        foreach (var localeFile in Directory.GetFiles(Path.Combine(_localesPath, "messages")))
        {
            var fileName = Path.GetFileNameWithoutExtension(localeFile);
            var locales = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(localeFile, stoppingToken))
                          ?? throw new InvalidOperationException();

            foreach (var (key, value) in locales)
            {
                _localisationService.AddOrUpdateLocale(fileName, key, value);
                valuesCounter++;
            }

            counter++;
        }

        sw.Stop();
        _logger.LogInformation("Loaded {Count} locales with total values of {ValuesCount} in {Elapsed}ms", counter, valuesCounter, sw.ElapsedMilliseconds);


        // Loading text command locales
        sw.Restart();
        counter = 0;
        valuesCounter = 0;

        foreach (var commandLocaleFile in Directory.GetFiles(Path.Combine(_localesPath, "text_commands")))
        {
            var fileName = Path.GetFileNameWithoutExtension(commandLocaleFile);
            var textCommandLocales = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(commandLocaleFile, stoppingToken))
                                     ?? throw new InvalidOperationException();

            foreach (var (key, value) in textCommandLocales)
            {
                _localisationService.AddOrUpdateTextCommandLocale(fileName, key, value);
                valuesCounter++;
            }

            counter++;
        }

        sw.Stop();
        _logger.LogInformation("Loaded {Count} text command locales with total values of {ValuesCount} in {Elapsed}ms", counter, valuesCounter, sw.ElapsedMilliseconds);

        // Loading guild locales
        sw.Restart();
        counter = 0;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        var guildEntities = await db.Guilds.AsNoTracking().Where(guildDb => !string.IsNullOrEmpty(guildDb.Locale)).ToListAsync(stoppingToken);

        foreach (var guildEntity in guildEntities)
        {
            _localisationService.AddOrUpdateGuildLocale(guildEntity.GuildId, guildEntity.Locale!);
            counter++;
        }

        sw.Stop();
        _logger.LogInformation("Loaded locales for {Count} guilds in {Elapsed}ms", counter, sw.ElapsedMilliseconds);
    }
}