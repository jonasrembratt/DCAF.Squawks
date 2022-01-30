using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using TetraPak.DynamicEntities;

namespace DCAF.Squawks
{
    public class LotAtcSquawksParser
    {
        readonly Variables _variables;

        public async Task<string> ParseAsync(FileInfo file)
        {
            if (!file.Exists)
                throw new FileNotFoundException($"File was not found: {file.FullName}");

            var jsonPreprocessor = new JsonProcessor();
            await using var stream = await jsonPreprocessor.LoadAndCleanJsonAsync(file);
            try
            {
                var root = await JsonSerializer.DeserializeAsync<LotAtcRoot>(stream, new JsonSerializerOptions
                {
                    AllowTrailingCommas = true
                });

                if (root is null)
                    throw new SerializationException($"Failed to parse JSON in file: {file.FullName} (no result)");
            
                return toJson(expandSquawkRangesAndSubstituteVariables(root));
            }
            catch (JsonException ex)
            {
                throw new Exception($"In template JSON: {ex.Message}");
            }
        }

        static string toJson(LotAtcRoot root) => JsonSerializer.Serialize(root, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        LotAtcRoot expandSquawkRangesAndSubstituteVariables(LotAtcRoot rootIn)
        {
            // note Currently we're only supporting Mode 3 ...
            var transponders = rootIn.Transponders is { } 
                ? processTransponders() 
                : new List<Transponder>();
            return new LotAtcRoot
            {
                Comments = @"File was processed by DCAF LotATC squawk compiler (please contact Jonas ""Wife"" Rembratt for issues or questions)",
                Enable = rootIn.Enable,
                Transponders = transponders
            };
            
            List<Transponder> processTransponders()
            {
                var list = new List<Transponder>();
                var noCounters = new Dictionary<string, Counter>();
                foreach (var transponder in rootIn.Transponders)
                {
                    var mode3 = transponder.Mode3;
                    if (string.IsNullOrEmpty(mode3) || !isRange(mode3, out var range))
                    {
                        list.Add(substituteDynamicValues(transponder, noCounters));
                        continue;
                    }
            
                    range!.SwapIfNeeded();
                    for (var i = range.From; i <= range.To; i += range.Increment)
                    {
                        var intValue = i;
                        var clone = substituteDynamicValues(transponder.Clone<Transponder>(), range.Counters, "mode3");
                        clone.Mode3 = intValue.ToString("0000");
                        list.Add(clone);
                    }
                }
            
                return list;
            }
        }

        T substituteDynamicValues<T>(T entity, IDictionary<string,Counter> counters, params string[] ignoreKeys) 
        where T : DynamicEntity
        {
            var ignoreHash = ignoreKeys.ToHashSet();
            foreach (var (key, value) in entity)
            {
                if (ignoreHash.Contains(key))
                    continue;
                
                entity[key] = value switch
                {
                    DynamicEntity de => substituteDynamicValues(de, counters),
                    string s => _variables.SubstituteAll(s, counters),
                    _ => value
                };
            }

            return entity;
        }
        
        static bool isRange(string? squawk, out SquawkRange? range)
        {
            const string Separator = "-"; 
            
            if (string.IsNullOrEmpty(squawk) || !squawk.Contains(Separator))
            {
                range = null;
                return false;
            }
        
            var sepAt = squawk.IndexOf(Separator, StringComparison.Ordinal);
            var from = squawk[..sepAt].Trim();
            var to = squawk[(sepAt + 1)..].Trim();
            var parsed = parseIncrementAndCounters();
            if (!int.TryParse(from.Trim(), out var intFrom) || !isValidSquawk(intFrom))
                throw invalidSquawkRange(squawk);
        
            if (!int.TryParse(to.Trim(), out var intTo) || !isValidSquawk(intTo))
                throw invalidSquawkRange(squawk);
        
            range = new SquawkRange
            {
                From = intFrom,
                To = intTo,
                Increment = parsed.increment,
                Counters = parsed.counters.ToDictionary(i => i.Name)
            };
            return true;
        
            (int increment, Counter[] counters) parseIncrementAndCounters()
            {
                var countersAt = to.IndexOf("#(", StringComparison.Ordinal);
                Counter[] counters;
                if (countersAt != -1)
                {
                    counters = Counter.Parse(to[countersAt..]);
                    to = to[..countersAt];
                }
                else
                {
                    counters = Array.Empty<Counter>();
                }
                
                var split = to.Split('+', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 1)
                    return (1, counters);

                to = split[0].Trim();
                var text = split[1].Trim();
                if (!int.TryParse(text, out var inc))
                    throw invalidSquawkRangeIncrement(squawk);
        
                return (Math.Abs(inc), counters);
            }
            
        }

        static bool isValidSquawk(int squawk) => squawk is >= 0 and <= 7777;

        static Exception invalidSquawkRange(string squawk) => new FormatException($"Invalid squawk range: {squawk}");

        static Exception invalidSquawkRangeIncrement(string squawk) => new FormatException($"Invalid squawk range increment: {squawk}");
        
        public LotAtcSquawksParser(Variables variables)
        {
            _variables = variables;
        }
    }
}