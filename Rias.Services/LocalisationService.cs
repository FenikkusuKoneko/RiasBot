using System.Collections.Concurrent;
using Disqord;

namespace Rias.Services;

public class LocalisationService
{
    private const string DefaultLocale = "en";
    
    // first string is the locale, second string is the key, third string is the value
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _locales = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _textCommandLocales = new();

    // first snowflake is the guild id, second string is the locale
    private readonly ConcurrentDictionary<Snowflake, string> _guildLocales = new();

    public void AddOrUpdateLocale(string locale, string key, string value)
    {
        if (_locales.TryGetValue(locale, out var locales))
        {
            locales[key] = value;
        }
        else
        {
            _locales[locale] = new ConcurrentDictionary<string, string>
            {
                [key] = value
            };
        }
    }
    
    public void AddOrUpdateTextCommandLocale(string locale, string key, string value)
    {
        if (_textCommandLocales.TryGetValue(locale, out var locales))
        {
            locales[key] = value;
        }
        else
        {
            _textCommandLocales[locale] = new ConcurrentDictionary<string, string>
            {
                [key] = value
            };
        }
    }
    
    public string GetGuildLocale(Snowflake? guildId)
    {
        if (!guildId.HasValue)
            return DefaultLocale;

        return _guildLocales.TryGetValue(guildId.Value, out var locale) ? locale : DefaultLocale;
    }

    public void AddOrUpdateGuildLocale(Snowflake guildId, string locale)
        => _guildLocales[guildId] = locale;

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