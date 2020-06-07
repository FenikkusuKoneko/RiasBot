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
using Rias.Core.Attributes;
using Rias.Core.Models;
using Serilog;
using StackExchange.Redis;

namespace Rias.Core.Services
{
    [AutoStart]
    public class UnitsService : RiasService
    {
        private readonly HttpClient _httpClient;
        private readonly IDatabase _redisDb;
        
        private readonly ConcurrentDictionary<string, UnitsModel> _units = new ConcurrentDictionary<string, UnitsModel>();
        private readonly string _unitsPath = Path.Combine(Environment.CurrentDirectory, "assets/units");
        private const string ExchangeRatesApi = "https://api.exchangeratesapi.io/latest";

        public UnitsService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            _redisDb = serviceProvider.GetRequiredService<ConnectionMultiplexer>().GetDatabase();
            var sw = Stopwatch.StartNew();
            
            foreach (var unitsFile in Directory.GetFiles(_unitsPath))
            {
                var units = JsonConvert.DeserializeObject<UnitsModel>(File.ReadAllText(unitsFile));
                foreach (var unit in units.Units!)
                    unit.Category = units;
                
                _units.TryAdd(units.Name.ToLower(), units);
            }
            
            sw.Stop();
            Log.Debug($"Units loaded: {sw.ElapsedMilliseconds} ms");

            RunTaskAsync(UpdateCurrencyUnitsAsync());
        }

        public double Convert(Unit unit1, Unit unit2, double value)
        {
            var expr = new Expression(unit1.FuncToBase);
            expr.EvaluateParameter += (name, args) => ExpressionEvaluateParameter(name, args, value);
            var baseResult = (double) expr.Evaluate();
            
            expr = new Expression(unit2.FuncFromBase);
            expr.EvaluateParameter += (name, args) => ExpressionEvaluateParameter(name, args, baseResult);
            return (double) expr.Evaluate();
        }

        public IEnumerable<UnitsModel> GetAllUnits()
            => _units.Values.ToList();

        public UnitsModel? GetUnitsByCategory(string category)
            => _units.TryGetValue(category.ToLower(), out var units) ? units : null;

        public IEnumerable<Unit> GetUnits(string name, bool noSpaces)
        {
            if (name.Length <= 3) return GetUnitsByAbbreviation(name);
            
            var unit = GetUnit(name, noSpaces);
            return unit != null ? new[] {unit} : Enumerable.Empty<Unit>();
        }

        private Unit? GetUnit(string name, bool noSpaces)
        {
            foreach (var (_, unitsModel) in _units)
            {
                var unit = unitsModel.Units.FirstOrDefault(x =>
                    string.Equals(noSpaces ? x.Name.Singular.Replace(" ", "") : x.Name.Singular, name, StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(noSpaces ? x.Name.Plural.Replace(" ", "") : x.Name.Plural, name, StringComparison.InvariantCultureIgnoreCase));
                
                if (unit != null)
                    return unit;
            }
            
            return null;
        }

        private IEnumerable<Unit> GetUnitsByAbbreviation(string abbreviation)
        {
            abbreviation = abbreviation.ToLower();
            foreach (var (_, unitsModel) in _units)
            {
                var unit = unitsModel.Units.FirstOrDefault(x => x.Name.Abbreviations != null
                    && x.Name.Abbreviations.Any(y => string.Equals(y, abbreviation, StringComparison.InvariantCultureIgnoreCase)));
                if (unit != null)
                    yield return unit;
            }
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
                
                //ignore EUR because it's the base
                if (string.Equals(unitAbbreviation, "EUR"))
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
        
        private static void ExpressionEvaluateParameter(string name, ParameterArgs args, double value)
        {
            args.Result = name.ToLowerInvariant() switch
            {
                "x" => value,
                _ => default
            };
        }
    }
}