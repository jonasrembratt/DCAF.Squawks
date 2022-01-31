using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DCAF.Squawks
{
    [DebuggerDisplay("{ToString()}")]
    class Counter
    {
        internal const string DeclaringQualifier = "#(";
        internal const string ReferenceQualifier = "=(";
        internal const char Suffix = ')';
        const char StartQualifier = '=';
        const char IncrementQualifier = '+';
        const int DefaultStart = 0;
        const int DefaultIncrement = 1;
        
        int _nextValue;

        public string Name { get;  private set;}
        
        public int Start { get; private set;  }

        public int Increment { get; private set; }

        public int NextValue => getNextValue();

        int getNextValue()
        {
            var value = _nextValue;
            _nextValue += Increment;
            return value;
        }

        public override string ToString() => $"{DeclaringQualifier}{Name} {StartQualifier} {Start} {IncrementQualifier} {Increment}{Suffix}";

        public static Counter[] Parse(string s)
        {
            var ca = s.ToCharArray();
            var list = new List<Counter>();
            Counter? counter = null;
            var i = 0;
            for (; i < ca.Length; i++)
            {
                var c = ca[i];
                if (char.IsWhiteSpace(c))
                    continue;

                if (isQualifier())
                {
                    if (counter is { })
                        throw new FormatException($"Expected end of counter here: {s}");

                    var name = parseName();
                    var start = parseStart();
                    var increment = parseIncrement();
                    list.Add(new Counter(name, start, increment));
                }
            }

            return list.ToArray();
            
            string parseName()
            {
                i += DeclaringQualifier.Length;
                var sb = new StringBuilder();
                for (; i < ca.Length; i++)
                {
                    var c = ca[i];
                    if (isIdentifier(c))
                    {
                        sb.Append(ca[i]);
                        continue;
                    }

                    if (sb.Length == 0)
                        throw new FormatException($"Counter name is missing: {s}");

                    if (c is StartQualifier or IncrementQualifier or Suffix)
                        return sb.ToString();
                }
                throw new FormatException($"Invalid counter here: {s}");
            }

            int parseStart()
            {
                var c = ca[i];
                if (c is IncrementQualifier or Suffix)
                    return DefaultStart;
                
                var sb = new StringBuilder();
                for (; i < ca.Length; ++i)
                {
                    c = ca[i];
                    if (char.IsWhiteSpace(c))
                        continue;
                    
                    if (char.IsDigit(c))
                    {
                        sb.Append(ca[i]);
                        continue;
                    }

                    if (sb.Length == 0)
                        throw new FormatException($"Counter start value is missing: {s}");

                    if (c is Suffix or IncrementQualifier)
                        return int.Parse(sb.ToString().Trim());
                }
                throw new FormatException($"Invalid counter here: {s}");
            }
            
            int parseIncrement()
            {
                if (ca[i] == Suffix)
                    return DefaultIncrement;

                var sb = new StringBuilder();
                for (++i; i < ca.Length; ++i)
                {
                    var c = ca[i];
                    if (char.IsWhiteSpace(c))
                        continue;
                    
                    if (char.IsDigit(c))
                    {
                        sb.Append(ca[i]);
                        continue;
                    }

                    if (c == Suffix)
                        return int.Parse(sb.ToString().Trim());
                }
                throw new FormatException($"Invalid counter here: {s}");
            }
            
            bool isQualifier() => i < s.Length - 2 && ca[i] == DeclaringQualifier[0] && ca[i + 1] == DeclaringQualifier[1];
            
            bool isIdentifier(char c) => char.IsLetter(c) || c == '_';

        }

        Counter(string name, int start, int increment)
        {
            Name = name;
            Start = start;
            Increment = increment;
            _nextValue = Start;
        }

        public static string ToInstanceString(string name) => $"{DeclaringQualifier}{name}{Suffix}";
    }
}