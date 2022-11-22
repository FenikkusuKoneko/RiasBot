using System.Collections.Concurrent;
using System.Diagnostics;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rias.Database;

namespace Rias.Services;

public class LocalisationService
{
    public const string DefaultLocale = "en";
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LocalisationService> _logger;

    // first string is the locale, second string is the key, third string is the value
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _locales = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _textCommandLocales = new();

    // first snowflake is the guild id, second string is the locale
    private readonly ConcurrentDictionary<Snowflake, string> _guildLocales = new();
    private readonly string _localesPath = Path.Combine(Environment.CurrentDirectory, "assets/l10n");

    public LocalisationService(IServiceProvider serviceProvider, ILogger<LocalisationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public async Task LoadAsync()
    {
        // Loading general locales
        var sw = Stopwatch.StartNew();
        
        _locales.Clear();
        foreach (var localeFile in Directory.GetFiles(Path.Combine(_localesPath, "messages")))
        {
            var fileName = Path.GetFileNameWithoutExtension(localeFile);
            var locales = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(await File.ReadAllTextAsync(localeFile)) 
                          ?? throw new InvalidOperationException();
            
            _locales.TryAdd(fileName, locales);
        }
        
        sw.Stop();
        _logger.LogInformation("Loaded {Count} locales in {Elapsed}ms", _locales.Count, sw.ElapsedMilliseconds);
        
        
        // Loading text command locales
        sw.Restart();
        
        _textCommandLocales.Clear();
        foreach (var commandLocaleFile in Directory.GetFiles(Path.Combine(_localesPath, "text_commands")))
        {
            var fileName = Path.GetFileNameWithoutExtension(commandLocaleFile);
            var textCommandLocales = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(await File.ReadAllTextAsync(commandLocaleFile)) 
                          ?? throw new InvalidOperationException();
            
            _textCommandLocales.TryAdd(fileName, textCommandLocales);
        }
        
        sw.Stop();
        _logger.LogInformation("Loaded {Count} text command locales in {Elapsed}ms", _textCommandLocales.Count, sw.ElapsedMilliseconds);
        
        // Loading guild locales
        sw.Restart();
        
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
        var guildEntities = await db.Guilds.Where(guildDb => !string.IsNullOrEmpty(guildDb.Locale)).ToListAsync();
        
        _guildLocales.Clear();
        foreach (var guildEntity in guildEntities)
            _guildLocales.TryAdd(guildEntity.GuildId, guildEntity.Locale!);
        
        sw.Stop();
        _logger.LogInformation("Loaded {Count} guild locales in {Elapsed}ms", _guildLocales.Count, sw.ElapsedMilliseconds);
    }
    
    public string GetGuildLocale(Snowflake? guildId)
    {
        if (!guildId.HasValue)
            return DefaultLocale;

        return _guildLocales.TryGetValue(guildId.Value, out var locale) ? locale : DefaultLocale;
    }
    
    public void SetGuildLocale(Snowflake guildId, string locale)
    {
        _guildLocales.AddOrUpdate(guildId, locale, (_, _) => locale);
    }
    
    public void RemoveGuildLocale(Snowflake guildId)
    {
        _guildLocales.TryRemove(guildId, out _);
    }

    /// <summary>
    /// Get a translation string without arguments.
    /// </summary>
    public string GetText(Snowflake? guildId, string key)
    {
        var locale = guildId.HasValue ? GetGuildLocale(guildId.Value) : DefaultLocale;
        if (TryGetLocaleString(locale, key, out var @string) && !string.IsNullOrEmpty(@string))
            return @string;

        if (!string.Equals(locale, DefaultLocale) && TryGetLocaleString(DefaultLocale, key, out @string) && !string.IsNullOrEmpty(@string))
            return @string;

        throw new InvalidOperationException($"The translation for the key \"{key}\" couldn't be found.");
    }
    
    /// <summary>
    /// Get a translation string with one argument.
    /// </summary>
    public string GetText(Snowflake? guildId, string key, object arg0)
        => string.Format(GetText(guildId, key), arg0);
    
    /// <summary>
    /// Get a translation string with two arguments.
    /// </summary>
    public string GetText(Snowflake? guildId, string key, object arg0, object arg1)
        => string.Format(GetText(guildId, key), arg0, arg1);
    
    /// <summary>
    /// Get a translation string with three arguments.
    /// </summary>
    public string GetText(Snowflake? guildId, string key, object arg0, object arg1, object arg2)
        => string.Format(GetText(guildId, key), arg0, arg1, arg2);

    /// <summary>
    /// Get a translation string with arguments.
    /// </summary>
    public string GetText(Snowflake? guildId, string key, params object[] args)
        => string.Format(GetText(guildId, key), args);

    public string? GetCommandText(Snowflake? guildId, string key)
    {
        var locale = guildId.HasValue ? GetGuildLocale(guildId.Value) : DefaultLocale;
        if (TryGetTextCommandLocaleString(locale, key, out var text) && !string.IsNullOrEmpty(text))
            return text;

        if (!string.Equals(locale, DefaultLocale) && TryGetTextCommandLocaleString(DefaultLocale, key, out text) && !string.IsNullOrEmpty(text))
            return text;

        return null;
    }

    private bool TryGetLocaleString(string locale, string key, out string? value)
    {
        if (_locales.TryGetValue(locale, out var localeDictionary))
        {
            if (localeDictionary.TryGetValue(key, out var @string))
            {
                value = @string;
                return true;
            }
        }

        value = null;
        return false;
    }
    
    private bool TryGetTextCommandLocaleString(string locale, string key, out string? value)
    {
        if (_textCommandLocales.TryGetValue(locale, out var localeDictionary))
        {
            if (localeDictionary.TryGetValue(key, out var @string))
            {
                value = @string;
                return true;
            }
        }

        value = null;
        return false;
    }
}