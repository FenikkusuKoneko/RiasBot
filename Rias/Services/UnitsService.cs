using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rias.Attributes;
using Rias.Models;
using Serilog;
using StackExchange.Redis;

namespace Rias.Services
{
    [AutoStart]
    public class UnitsService : RiasService
    {
        private const string ExchangeRatesApi = "https://api.exchangeratesapi.io/latest";
        private static readonly string UnitsPath = Path.Combine(Environment.CurrentDirectory, "assets/units");

        private readonly HttpClient _httpClient;
        private readonly IDatabase _redisDb;

        private readonly ConcurrentDictionary<string, UnitsCategory> _units = new ConcurrentDictionary<string, UnitsCategory>();
        private readonly ConcurrentDictionary<string, List<Unit>> _unitsSingular = new ConcurrentDictionary<string, List<Unit>>();
        private readonly ConcurrentDictionary<string, List<Unit>> _unitsPlural = new ConcurrentDictionary<string, List<Unit>>();
        private readonly ConcurrentDictionary<string, List<Unit>> _unitsAbbreviations = new ConcurrentDictionary<string, List<Unit>>();

        public UnitsService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            _redisDb = serviceProvider.GetRequiredService<ConnectionMultiplexer>().GetDatabase();
            var sw = Stopwatch.StartNew();
            
            foreach (var unitsFile in Directory.GetFiles(UnitsPath))
            {
                var unitsCategory = JsonConvert.DeserializeObject<UnitsCategory>(File.ReadAllText(unitsFile));
                foreach (var unit in unitsCategory.Units)
                {
                    unit.Category = unitsCategory;
                    var nameSingular = unit.Name.Singular.ToLower().Replace(" ", string.Empty);
                    if (_unitsSingular.TryGetValue(nameSingular, out var unitsSingular))
                        unitsSingular.Add(unit);
                    else
                        _unitsSingular[nameSingular] = new List<Unit> { unit };
                    
                    var namePlural = unit.Name.Plural.ToLower().Replace(" ", string.Empty);
                    if (_unitsPlural.TryGetValue(namePlural, out var unitsPlural))
                        unitsPlural.Add(unit);
                    else
                        _unitsPlural[namePlural] = new List<Unit> { unit };

                    if (unit.Name.Abbreviations is null)
                        continue;

                    var isCurrencyCategory = unitsCategory.Name.Equals("currency", StringComparison.CurrentCultureIgnoreCase);
                    foreach (var abbreviation in unit.Name.Abbreviations)
                    {
                        var abb = isCurrencyCategory ? abbreviation.ToLower() : abbreviation; 
                        if (_unitsAbbreviations.TryGetValue(abb, out var unitsAbbreviations))
                            unitsAbbreviations.Add(unit);
                        else
                            _unitsAbbreviations[abb] = new List<Unit> { unit };
                    }
                }
                
                _units.TryAdd(unitsCategory.Name.ToLower(), unitsCategory);
            }
            
            sw.Stop();
            Log.Debug($"Units loaded: {sw.ElapsedMilliseconds} ms");

            RunTaskAsync(UpdateCurrencyUnitsAsync());
        }

        public double Convert(Unit unit1, Unit unit2, double value)
        {
            var expr = new Expression(unit1.FuncToBase);
            expr.EvaluateParameter += (name, args) => ExpressionEvaluateParameter(name, args, value);
            var baseResult = (double)expr.Evaluate();
            
            expr = new Expression(unit2.FuncFromBase);
            expr.EvaluateParameter += (name, args) => ExpressionEvaluateParameter(name, args, baseResult);
            return (double)expr.Evaluate();
        }

        public IEnumerable<UnitsCategory> GetAllUnits()
            => _units.Values.ToList();

        public UnitsCategory? GetUnitsByCategory(string category)
            => _units.TryGetValue(category.ToLower(), out var units) ? units : null;

        public IEnumerable<Unit> GetUnits(string name)
        {
            if (name.Length <= 5 && _unitsAbbreviations.TryGetValue(name, out var unitsAbbreviations))
                return unitsAbbreviations;
            
            name = name.ToLower().Replace(" ", string.Empty);
            return _unitsSingular.TryGetValue(name, out var unitsSingular)
                ? unitsSingular
                : _unitsPlural.TryGetValue(name, out var unitsPlural)
                    ? unitsPlural
                    : Enumerable.Empty<Unit>();
        }
        
        private static void ExpressionEvaluateParameter(string name, ParameterArgs args, double value)
        {
            args.Result = name.ToLowerInvariant() switch
            {
                "x" => value,
                _ => default
            };
        }
        
        private async Task UpdateCurrencyUnitsAsync()
        {
            var exchangeRatesDataRedis = _redisDb.StringGetWithExpiry("converter:currency");
            var exchangeRatesData = !exchangeRatesDataRedis.Value.IsNullOrEmpty
                ? exchangeRatesDataRedis.Value.ToString()
                : await _httpClient.GetStringAsync(ExchangeRatesApi);
            
            if (exchangeRatesDataRedis.Expiry is null)
                await _redisDb.StringSetAsync("converter:currency", exchangeRatesData, TimeSpan.FromMinutes(59));
            
            var exchangeRates = JsonConvert.DeserializeObject<JObject>(exchangeRatesData)["rates"]?
                .ToObject<Dictionary<string, double>>();

            var currencyUnits = _units["currency"];
            foreach (var unit in currencyUnits.Units)
            {
                var unitAbbreviation = unit.Name.Abbreviations.ElementAt(0);
                
                // ignore EUR because it's the base
                if (string.Equals(unitAbbreviation, "eur", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                var rateValue = exchangeRates![unitAbbreviation];
                unit.FuncToBase = $"x / ${rateValue}";
                unit.FuncFromBase = $"x * ${rateValue}";
            }
            
            Log.Information("Currency units updated");

            var delay = exchangeRatesDataRedis.Expiry ?? TimeSpan.FromHours(1);
            await Task.Delay(delay);
            await RunTaskAsync(UpdateCurrencyUnitsAsync());
        }
    }
}