using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DCAF.Squawks
{
    public class Variables
    {
        const string Qualifier = "$(";
        // const char Qualifier1 = '$'; obsolete
        // const char Qualifier2 = '(';
        const char Suffix = ')';
        
        readonly Dictionary<string,string>? _dictionary;

        internal string? SubstituteAll(string? text, IDictionary<string, Counter> counters)
        {
            if (/*_dictionary is null || obsolete */ text is null)
                return text;

            var containsCounters = text.Contains(Counter.ReferenceQualifier);
            if (containsCounters)
            {
                // counters must be resolved before dynamic value as they might be part of the value identifiers ...
                text = substituteAllCounters();
            }

            return substituteAllValues();

            string substituteAllCounters()
            {
                var ca = text.ToCharArray();
                var sb = new StringBuilder();
                for (var i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    if (!isCounterReference(ca, ref i, out var name))
                    {
                        sb.Append(c);
                        continue;
                    }

                    if (!counters.TryGetValue(name, out var counter))
                    {
                        // counter was not found 
                        sb.Append($"WARNING! Unrecognized: {Counter.ToInstanceString(name)}");
                        continue;
                    }

                    sb.Append(counter.NextValue);
                }

                return sb.ToString();
            }

            string substituteAllValues()
            {
                var ca = text.ToCharArray();
                var sb = new StringBuilder();
                for (var i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    if (isVariable(ca, ref i, out var value) || value is { })
                    {
                        sb.Append(value);
                        continue;
                    }

                    sb.Append(c);
                }

                return sb.ToString();
            }

            static bool isCounterReference(char[] ca, ref int i, [NotNullWhen(true)] out string? name)
            {
                if (i > ca.Length - 4 || ca[i] != Counter.ReferenceQualifier[0] ||
                    ca[i + 1] != Counter.ReferenceQualifier[1])
                {
                    name = null;
                    return false;
                }

                var sb = new StringBuilder();
                for (var j = i + 2; j < ca.Length; j++)
                {
                    var c = ca[j];
                    if (c == Counter.Suffix)
                    {
                        name = sb.ToString();
                        i = j;
                        return true;
                    }

                    sb.Append(c);
                }

                name = null;
                return false;
            }
        }

        bool isVariable(char[] ca, ref int index, out string? value)
        {
            if (!ca.IsToken(index, Qualifier))
            {
                value = null;
                return false;
            }

            var sb = new StringBuilder();
            var isEmpty = true;
            for (var i = index+2; i < ca.Length; i++)
            {
                var c = ca[i];
                if (c == Suffix)
                {
                    if (isEmpty)
                    {
                        value = string.Empty;
                        index += 2;
                        return false;
                    }

                    var key = sb.ToString();
                    if (_dictionary is {} && _dictionary.TryGetValue(key, out value))
                    {
                        // send back the variable value ...
                        index += key.Length + 2;
                        return true;
                    }

                    // key wasn't in dictionary; just send back the variable identifier as-is ...
                    value = key;
                    index += key.Length + 2;
                    return false;
                }

                sb.Append(c);
                isEmpty = false;
            } 
            
            // reached end of text without finding a ')' suffix; return everything as-is ...
            value = $"{Qualifier}{sb}";
            return false;
        }


        public Variables(Dictionary<string,string>? dictionary)
        {
            _dictionary = dictionary;
        }
    }
}