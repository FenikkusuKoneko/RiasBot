using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Serilog;

namespace Rias.Core.Implementation
{
    public class Resources
    {
        private readonly ConcurrentDictionary<ulong, CultureInfo> _guildCultures = new ConcurrentDictionary<ulong, CultureInfo>();
        private readonly ResourceManager _resources = new ResourceManager("Rias.Properties.Resources", Assembly.Load("Rias"));
        private readonly CultureInfo _defaultCulture = new CultureInfo("en");

        public Resources(IServiceProvider services)
        {
            var sw = new Stopwatch();
            sw.Start();

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildLocalesDb = db.Guilds.Where(guildDb => !string.IsNullOrEmpty(guildDb.Locale)).ToList();
            foreach (var localeDb in guildLocalesDb)
            {
                _guildCultures.TryAdd(localeDb.GuildId, new CultureInfo(localeDb.Locale!));
            }

            sw.Stop();
            Log.Information($"Resources loaded: {sw.ElapsedMilliseconds} ms");
        }

        public void SetGuildCulture(ulong guildId, CultureInfo culture)
        {
            _guildCultures.AddOrUpdate(guildId, culture, (id, old) => culture);
        }

        public CultureInfo GetGuildCulture(ulong? guildId)
        {
            if (!guildId.HasValue)
                return _defaultCulture;

            return _guildCultures.TryGetValue(guildId.Value, out var culture) ? culture : _defaultCulture;
        }

        public void RemoveGuildCulture(ulong guildId)
        {
            _guildCultures.TryRemove(guildId, out _);
        }

        private string GetText(ulong? guildId, string key, params object[] args)
        {
            var culture = guildId.HasValue ? GetGuildCulture(guildId.Value) : _defaultCulture;
            var resourceString = _resources.GetString(key, culture);
            if (string.IsNullOrEmpty(resourceString))
                throw new InvalidOperationException($"The translation for the key \"{key}\" couldn't be found.");
            return string.Format(resourceString, args);
        }

        /// <summary>
        /// Get a translation string with or without arguments.<br/>
        /// </summary>
        public string GetText(ulong? guildId, string? prefix, string key, params object[] args)
        {
            return GetText(guildId, $"{prefix}_{key}", args);
        }
        
        public readonly (string Locale, string Language)[] Languages =
        {
            ("EN", "English")
        };
    }
}