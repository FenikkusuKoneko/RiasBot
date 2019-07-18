using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace Rias.Core.Implementation
{
    public class Translations
    {
        private readonly ConcurrentDictionary<ulong, string> _guildLocales = new ConcurrentDictionary<ulong, string>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _translations =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

        public Translations()
        {
            var sw = new Stopwatch();
            sw.Start();

            var translationsPath = Path.Combine(Environment.CurrentDirectory, "assets/translations");
            var translations = Directory.GetFiles(translationsPath);
            
            foreach (var translation in translations)
            {
                var translationKey = Path.GetFileName(translation).Replace(".json", "");
                var translationValue = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText(translation));
                _translations.TryAdd(translationKey, translationValue);
            }

            sw.Stop();
            Log.Information($"Translations loaded: {sw.ElapsedMilliseconds} ms");
        }
        
        public void SetGuildLocale(ulong guildId, string locale)
        {
            _guildLocales.AddOrUpdate(guildId, locale, (id, old) => locale);
        }

        public string GetGuildLocale(ulong guildId)
        {
            return _guildLocales.TryGetValue(guildId, out var locale) ? locale : "en-US";
        }

        public void RemoveGuildLocale(ulong guildId)
        {
            _guildLocales.TryRemove(guildId, out _);
        }

        private string GetText(ulong? guildId, string key)
        {
            if (guildId.HasValue)
            {
                var locale = GetGuildLocale(guildId.Value);
                if (!_translations.TryGetValue(locale, out var strings))
                    throw new InvalidOperationException("The locale of the guild is invalid.");

                if (strings.TryGetValue(key, out var translation)) return translation;
            }

            if (!_translations.TryGetValue("en-US", out var enStrings))
                throw new InvalidOperationException("The translation strings for the english locale couldn't be found.");
            return enStrings.TryGetValue(key, out var enTranslation) ? enTranslation
                : throw new InvalidOperationException($"The translation for the key \"{key}\" couldn't be found.");
        }

        /// <summary>
        /// Get a translation string.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation and <paramref name="prefix"/> will be ignored.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is <paramref name="prefix"/>.
        /// </summary>
        public string GetText(ulong? guildId, string prefix, string key)
        {
            return key.StartsWith("#") ? GetText(guildId, key.Remove(0, 1)) : GetText(guildId, prefix + "_" + key);
        }

        /// <summary>
        /// Get a translation string with arguments.<br/>
        /// If the key starts with "#", the first word delimited by "_" is the prefix for the translation and <paramref name="prefix"/> will be ignored.<br/>
        /// If the key doesn't start with "#", the prefix of the translation is <paramref name="prefix"/>.
        /// </summary>
        public string GetText(ulong? guildId, string prefix, string key, params object[] args)
        {
            return string.Format(key.StartsWith("#") ? GetText(guildId, key.Remove(0, 1)) : GetText(guildId, prefix + "_" + key), args);
        }
    }
}