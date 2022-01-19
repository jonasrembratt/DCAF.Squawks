using System.Collections.Generic;
using System.Text;

namespace DCAF.Squawks
{
    public class Variables
    {
        const char Qualifier1 = '$';
        const char Qualifier2 = '(';
        const char Suffix = ')';
        
        readonly Dictionary<string,string>? _dictionary;

        public string? SubstituteAll(string? text)
        {
            if (_dictionary is null || text is null)
                return text;

            var ca = text.ToCharArray();
            var sb = new StringBuilder();
            for (var i = 0; i < ca.Length; i++)
            {
                var c = ca[i];
                if (isVariable(ca, ref i, out var value) || value is {})
                {
                    sb.Append(value);
                    continue;
                }
                sb.Append(c);
            }

            return sb.ToString();
        }

        bool isVariable(IReadOnlyList<char> ca, ref int index, out string? value)
        {
            if (index >= ca.Count - 3 || ca[index] != Qualifier1 || ca[index+1] != Qualifier2)
            {
                value = null;
                return false;
            }

            var sb = new StringBuilder();
            var isEmpty = true;
            for (var i = index+2; i < ca.Count; i++)
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
                    if (_dictionary!.TryGetValue(key, out value))
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
            value = $"{Qualifier1}{Qualifier2}{sb}";
            return false;
        }


        public Variables(Dictionary<string,string>? dictionary)
        {
            _dictionary = dictionary;
        }
    }
}