using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rias.Attributes;
using Rias.Implementation;
using Rias.Models;
using Serilog;
using StackExchange.Redis;

namespace Rias.Services
{
    [AutoStart]
    public class UnitsService : RiasService
    {
        private const string ExchangeRatesApi = "https://v6.exchangerate-api.com/v6/{0}/latest/USD";
        private static readonly string UnitsPath = Path.Combine(Environment.CurrentDirectory, "assets/units");

        private readonly HttpClient _httpClient;
        private readonly IDatabase _redisDb;

        private ImmutableDictionary<string, UnitsCategory> _units = null!;
        private ImmutableDictionary<string, SingleOrList<Unit>> _unitSingulars = null!;
        private ImmutableDictionary<string, SingleOrList<Unit>> _unitPlurals = null!;
        private ImmutableDictionary<string, SingleOrList<Unit>> _unitAbbreviations = null!;

        private CancellationTokenSource _updateCurrencyUnitsCts = new();

        public UnitsService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            _redisDb = serviceProvider.GetRequiredService<ConnectionMultiplexer>().GetDatabase();
            var sw = Stopwatch.StartNew();
            
            LoadUnits();

            sw.Stop();
            Log.Debug("Units loaded: {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);

            if (!string.IsNullOrEmpty(Configuration.ExchangeRateAccessKey))
                RunTaskAsync(UpdateCurrencyUnitsAsync);
        }

        public void ReloadUnits()
        {
            _updateCurrencyUnitsCts.Cancel();
            _updateCurrencyUnitsCts.Dispose();
            _updateCurrencyUnitsCts = new CancellationTokenSource();
            LoadUnits();
            
            if (!string.IsNullOrEmpty(Configuration.ExchangeRateAccessKey))
                RunTaskAsync(UpdateCurrencyUnitsAsync);
        }

        private void LoadUnits()
        {
            var units = new Dictionary<string, UnitsCategory>();
            var unitSingulars = new Dictionary<string, SingleOrList<Unit>>();
            var unitPlurals = new Dictionary<string, SingleOrList<Unit>>();
            var unitAbbreviations = new Dictionary<string, SingleOrList<Unit>>();
            
            foreach (var unitsFile in Directory.GetFiles(UnitsPath))
            {
                var unitsCategory = JsonConvert.DeserializeObject<UnitsCategory>(File.ReadAllText(unitsFile));
                
#if DEBUG || RIAS_GLOBAL
                if (string.Equals(unitsCategory!.Name, "currency", StringComparison.OrdinalIgnoreCase))
                {
                    var currencyUnits = unitsCategory.Units.ToList();
                    currencyUnits.Add(new Unit
                    {
                        Category = unitsCategory,
                        Name = new Unit.UnitName
                        {
                            Singular = "Heart",
                            Plural = "Hearts",
                            Abbreviations = new []{ "HRT" }
                        }
                    });
                    unitsCategory.Units = currencyUnits;
                }
#endif
                
                foreach (var unit in unitsCategory.Units)
                {
                    unit.Category = unitsCategory;
                    var nameSingular = unit.Name.Singular.ToLowerInvariant().Replace(" ", string.Empty);
                    if (unitSingulars.TryGetValue(nameSingular, out var unitSingular))
                        unitSingulars[nameSingular] = unitSingular.Add(unit);
                    else
                        unitSingulars[nameSingular] = new SingleOrList<Unit>(unit);
                    
                    var namePlural = unit.Name.Plural.ToLowerInvariant().Replace(" ", string.Empty);
                    if (unitPlurals.TryGetValue(namePlural, out var unitPlural))
                        unitPlurals[namePlural] = unitPlural.Add(unit);
                    else
                        unitPlurals[namePlural] = new SingleOrList<Unit>(unit);

                    if (unit.Name.Abbreviations is null)
                        continue;

                    foreach (var abbreviation in unit.Name.Abbreviations)
                    {
                        var abbreviationLowercase = abbreviation.ToLowerInvariant();
                        if (unitAbbreviations.TryGetValue(abbreviationLowercase, out var unitAbbreviation))
                            unitAbbreviations[abbreviationLowercase] = unitAbbreviation.Add(unit);
                        else
                            unitAbbreviations[abbreviationLowercase] = new SingleOrList<Unit>(unit);
                    }
                }

                units.TryAdd(unitsCategory.Name.ToLowerInvariant(), unitsCategory);
            }

            _units = units.ToImmutableDictionary();
            _unitSingulars = unitSingulars.ToImmutableDictionary();
            _unitPlurals = unitPlurals.ToImmutableDictionary();
            _unitAbbreviations = unitAbbreviations.ToImmutableDictionary();
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

        public IEnumerable<UnitsCategory> GetAllUnits()
            => _units.Values.ToList();

        public UnitsCategory? GetUnitsByCategory(string category)
            => _units.TryGetValue(category.ToLower(), out var units) ? units : null;

        public int GetUnits(ref string unitOneName, ref string unitTwoName, out Unit? unitOne, out Unit? unitTwo, UnitsCategory? category = null)
        {
            unitOne = null;
            unitTwo = null;
            
            var unitOneCaseSensitive = false;
            if (unitOneName[0] == '!')
            {
                unitOneCaseSensitive = true;
                unitOneName = unitOneName[1..];
            }
            
            var unitTwoCaseSensitive = false;
            if (unitTwoName[0] == '!')
            {
                unitTwoCaseSensitive = true;
                unitTwoName = unitTwoName[1..];
            }
            
            var unitsOne = category is null
                ? GetUnitsInternal(unitOneName, unitOneCaseSensitive)
                : GetUnitFromCategoryInternal(category, unitOneName, unitOneCaseSensitive);
            
            if (unitsOne is null)
                return -1;

            var unitsTwo = category is null
                ? GetUnitsInternal(unitTwoName, unitTwoCaseSensitive)
                : GetUnitFromCategoryInternal(category, unitTwoName, unitTwoCaseSensitive);
            
            if (unitsTwo is null)
                return -2;
            
            unitOne = unitsOne.Value.Value;
            unitTwo = unitsTwo.Value.Value;

            switch (unitOne)
            {
                case not null when unitTwo is not null:
                    return string.Equals(unitOne.Category.Name, unitTwo.Category.Name) ? 1 : 0;
                case not null when unitTwo is null:
                    unitTwo = GetCompatibleUnit(unitOne, unitsTwo.Value.List);
                    var index = unitTwo is not null ? 1 : 0;
                    unitTwo ??= unitsTwo.Value.List[0];
                    return index;
                case null when unitTwo is not null:
                    unitOne = GetCompatibleUnit(unitTwo, unitsOne.Value.List);
                    index = unitOne is not null ? 1 : 0;
                    unitOne ??= unitsOne.Value.List[0];
                    return index;
            }

            foreach (var u1 in unitsOne.Value.List)
            {
                foreach (var u2 in unitsTwo.Value.List)
                {
                    if (string.Equals(u1.Category.Name, u2.Category.Name))
                    {
                        unitOne = u1;
                        unitTwo = u2;
                        return 1;
                    }
                }
            }

            unitOne ??= unitsOne.Value.List[0];
            unitTwo ??= unitsTwo.Value.List[0];
            return 0;
        }

        private SingleOrList<Unit>? GetUnitsInternal(string name, bool caseSensitive)
        {
            var nameLowercase = name.ToLowerInvariant().Replace(" ", string.Empty);
            if (_unitSingulars.TryGetValue(nameLowercase, out var units))
                return units;
            
            if (_unitPlurals.TryGetValue(nameLowercase, out units))
                return units;

            if (_unitAbbreviations.TryGetValue(nameLowercase, out units))
            {
                if (!caseSensitive)
                    return units;

                if (units.Value?.Name.Abbreviations != null && units.Value.Name.Abbreviations.Any(abb => string.Equals(abb, name)))
                    return units;

                var unitsList = units.List?.Where(u => u.Name.Abbreviations is not null && u.Name.Abbreviations.Any(abb => string.Equals(abb, name))).ToList();
                if (unitsList is not null && unitsList.Count != 0)
                    return new SingleOrList<Unit>(unitsList);
            }

            return null;
        }

        private SingleOrList<Unit>? GetUnitFromCategoryInternal(UnitsCategory category, string name, bool caseSensitive)
        {
            var nameLowercase = name.ToLowerInvariant().Replace(" ", string.Empty);
            Unit? unitSingular = null;
            Unit? unitPlural = null;
            Unit? unitAbbreviation = null;

            foreach (var unit in category.Units)
            {
                if (unitSingular is null
                    && string.Equals(unit.Name.Singular, nameLowercase, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
                    unitSingular = unit;
                
                if (unitPlural is null
                    && string.Equals(unit.Name.Plural, nameLowercase, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
                    unitPlural = unit;

                if (unitAbbreviation is null
                    && unit.Name.Abbreviations is not null
                    && unit.Name.Abbreviations.Any(abb =>
                        string.Equals(abb, caseSensitive ? name : nameLowercase, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase)))
                    unitAbbreviation = unit;
            }

            if (unitSingular is not null)
                return new SingleOrList<Unit>(unitSingular);
            
            if (unitPlural is not null)
                return new SingleOrList<Unit>(unitPlural);
            
            if (unitAbbreviation is not null)
                return new SingleOrList<Unit>(unitAbbreviation);

            return null;
        }

        private Unit? GetCompatibleUnit(Unit unit, IEnumerable<Unit> units)
            => units.FirstOrDefault(x => string.Equals(x.Category.Name, unit.Category.Name));
        
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
                : await _httpClient.GetStringAsync(string.Format(ExchangeRatesApi, Configuration.ExchangeRateAccessKey));
            
            if (exchangeRatesDataRedis.Expiry is null)
                await _redisDb.StringSetAsync("converter:currency", exchangeRatesData, TimeSpan.FromHours(1));
            
            var exchangeRates = JsonConvert.DeserializeObject<JObject>(exchangeRatesData)!["conversion_rates"]?
                .ToObject<Dictionary<string, double>>();

            if (exchangeRates is null)
            {
                Log.Error("The \"conversion_rates\" field is not present in the exchange rates data!");
                return;
            }

            var currencyUnits = _units["currency"];
            foreach (var unit in currencyUnits.Units)
            {
                var unitAbbreviation = unit.Name.Abbreviations.ElementAt(0);

                if (string.Equals(unitAbbreviation, "usd", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(unitAbbreviation, "hrt", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!exchangeRates.TryGetValue(unitAbbreviation, out var rateValue))
                {
                    Log.Warning("The unit {Unit} is not present in the exchange rates dictionary", unitAbbreviation);
                    continue;
                }
                
                unit.FuncToBase = $"x / ${rateValue}";
                unit.FuncFromBase = $"x * ${rateValue}";
            }
            
#if DEBUG || RIAS_GLOBAL
            var heartsUnit = currencyUnits.Units.First(x => string.Equals(x.Name.Abbreviations.First(), "HRT"));

            heartsUnit.FuncToBase = "x / 500";
            heartsUnit.FuncFromBase = "x * 500";
#endif

            Log.Information("Currency units updated");

            var delay = exchangeRatesDataRedis.Expiry ?? TimeSpan.FromHours(1);
            await Task.Delay(delay, _updateCurrencyUnitsCts.Token);
            if (_updateCurrencyUnitsCts.IsCancellationRequested)
                return;
            
            await RunTaskAsync(UpdateCurrencyUnitsAsync);
        }
    }
}