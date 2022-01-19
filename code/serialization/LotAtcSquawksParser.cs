using System;
using System.Collections.Generic;
using System.IO;
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
                throw new FileNotFoundException($"File was not found: {file.FullName}", file.Name);

            await using var stream = file.OpenRead();
            var root = await JsonSerializer.DeserializeAsync<LotAtcRoot>(stream, new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            });

            if (root is null)
                throw new SerializationException($"Failed to parse JSON in file: {file.FullName} (no result)");
            
            return toJson(expandSquawkRangesAndSubstituteVariables(root));
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
                Comments = @"File was processed by DCAF LotATC squawk parser (please contact Jonas ""Wife"" Rembratt for issues or questions)",
                Enable = rootIn.Enable,
                Transponders = transponders
            };
            
            List<Transponder> processTransponders()
            {
                var list = new List<Transponder>();
                foreach (var transponder in rootIn.Transponders)
                {
                    var mode3 = transponder.Mode3;
                    if (string.IsNullOrEmpty(mode3) || !isRange(mode3, out var range))
                    {
                        list.Add(substituteVariables(transponder));
                        continue;
                    }
            
                    range!.SwapIfNeeded();
                    for (var i = range.From; i <= range.To; i += range.Increment)
                    {
                        var intValue = i;
                        var clone = substituteVariables(transponder.Clone<Transponder>());
                        clone.Mode3 = intValue.ToString("0000");
                        list.Add(clone);
                    }
                }
            
                return list;
            }
        }

        T substituteVariables<T>(T entity) where T : DynamicEntity
        {
            foreach (var (key, value) in entity)
            {
                entity[key] = value switch
                {
                    DynamicEntity de => substituteVariables(de),
                    string s => _variables.SubstituteAll(s),
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
            var increment = parseIncrement();
            if (!int.TryParse(from.Trim(), out var intFrom) || !isValidSquawk(intFrom))
                throw invalidSquawkRange(squawk);
        
            if (!int.TryParse(to.Trim(), out var intTo) || !isValidSquawk(intTo))
                throw invalidSquawkRange(squawk);
        
            range = new SquawkRange
            {
                From = intFrom,
                To = intTo,
                Increment = increment
            };
            return true;
        
            int parseIncrement()
            {
                var split = to.Split('+', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 1)
                    return 1;

                to = split[0].Trim();
                var text = split[1].Trim();
                if (!int.TryParse(text, out var inc))
                    throw invalidSquawkRangeIncrement(squawk);
        
                return Math.Abs(inc);
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