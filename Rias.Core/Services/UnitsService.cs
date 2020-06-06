using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NCalc;
using Newtonsoft.Json;
using Rias.Core.Attributes;
using Rias.Core.Models;
using Serilog;

namespace Rias.Core.Services
{
    [AutoStart]
    public class UnitsService : RiasService
    {
        private readonly ConcurrentDictionary<string, UnitsModel> _units = new ConcurrentDictionary<string, UnitsModel>();
        private readonly string _unitsPath = Path.Combine(Environment.CurrentDirectory, "assets/units");
        
        public UnitsService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            var sw = Stopwatch.StartNew();
            
            foreach (var unitsFile in Directory.GetFiles(_unitsPath))
            {
                var units = JsonConvert.DeserializeObject<UnitsModel>(File.ReadAllText(unitsFile));
                foreach (var unit in units.Units!)
                    unit.Category = units;
                
                _units.TryAdd(units.Name!.ToLower(), units);
            }

            sw.Stop();
            Log.Debug($"Units loaded: {sw.ElapsedMilliseconds} ms");
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

        public IEnumerable<Unit> GetUnits(string name)
        {
            if (name.Length <= 3) return GetUnitsByAbbreviation(name);
            
            var unit = GetUnit(name);
            return unit != null ? new[] {unit} : Enumerable.Empty<Unit>();
        }

        private Unit? GetUnit(string name)
        {
            foreach (var (_, unitsModel) in _units)
            {
                var unit = unitsModel.Units.FirstOrDefault(x => string.Equals(x.Name.Singular, name, StringComparison.InvariantCultureIgnoreCase)
                                                                || string.Equals(x.Name.Plural, name, StringComparison.InvariantCultureIgnoreCase));
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