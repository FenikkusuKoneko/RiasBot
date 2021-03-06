using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rias.Database;
using Serilog;

namespace Rias.Implementation
{
    public partial class Localization
    {
        public const string DefaultLocale = "en";

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _locales = new();

        private readonly ConcurrentDictionary<ulong, string> _guildLocales = new();
        private readonly string _localesPath = Path.Combine(Environment.CurrentDirectory, "assets/locales");

        public Localization(IServiceProvider serviceProvider)
        {
            var sw = Stopwatch.StartNew();
            Load(serviceProvider);
            sw.Stop();
            Log.Information("Locales loaded: {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
        }

        public void Reload(IServiceProvider serviceProvider)
        {
            _locales.Clear();
            _guildLocales.Clear();
            Load(serviceProvider);
        }

        private void Load(IServiceProvider serviceProvider)
        {
            foreach (var locale in Directory.GetFiles(_localesPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(locale);
                _locales.TryAdd(fileName, JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(locale))!);
            }
            
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();

            var guildLocalesDb = db.Guilds.Where(guildDb => !string.IsNullOrEmpty(guildDb.Locale)).ToList();
            foreach (var localeDb in guildLocalesDb)
            {
                _guildLocales.TryAdd(localeDb.GuildId, localeDb.Locale!);
            }
        }
        
        public void SetGuildLocale(ulong guildId, string locale)
        {
            _guildLocales.AddOrUpdate(guildId, locale, (_, _) => locale);
        }
        
        public string GetGuildLocale(ulong? guildId)
        {
            if (!guildId.HasValue)
                return DefaultLocale;

            return _guildLocales.TryGetValue(guildId.Value, out var locale) ? locale : DefaultLocale;
        }

        public void RemoveGuildLocale(ulong guildId)
        {
            _guildLocales.TryRemove(guildId, out _);
        }
        
        /// <summary>
        /// Get a translation string with or without arguments.<br/>
        /// </summary>
        public string GetText(ulong? guildId, string key, params object[] args)
        {
            var locale = guildId.HasValue ? GetGuildLocale(guildId.Value) : DefaultLocale;
            if (TryGetLocaleString(locale, key, out var @string) && !string.IsNullOrEmpty(@string))
                return string.Format(@string, args);

            if (!string.Equals(locale, DefaultLocale) && TryGetLocaleString(DefaultLocale, key, out @string) && !string.IsNullOrEmpty(@string))
                return string.Format(@string, args);

            throw new InvalidOperationException($"The translation for the key \"{key}\" couldn't be found.");
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
    }
}