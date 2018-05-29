using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad
{
    // originally from Tessin Deployment tool
    class Args
    {
        public static string[] Slice(string[] args, int offset)
        {
            var remainder = new string[args.Length - offset];
            Array.Copy(args, offset, remainder, 0, remainder.Length);
            return remainder;
        }

        private readonly string[] _formal;
        private readonly TypeCode[] _type;

        public Args(params string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var formal = new string[args.Length];
            var type = new TypeCode[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var split = arg.IndexOf(':');
                if (split == -1)
                {
                    formal[i] = arg;
                    type[i] = TypeCode.String;
                }
                else
                {
                    formal[i] = arg.Substring(0, split);
                    var s = arg.Substring(split + 1);
                    if (Enum.TryParse(s, /*ignoreCase:*/ true, out TypeCode t))
                    {
                        if (t == TypeCode.Empty)
                        {
                            throw new ArgumentException($"Type code '{t}' of formal parameter '{formal[i]}' is invalid.");
                        }
                        type[i] = t;
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot parse type code '{s}' of formal parameter '{formal[i]}'.");
                    }
                }
            }

            this._formal = formal;
            this._type = type;
        }

        public string[] Remainder { get; private set; }

        public bool Parse(string[] args)
        {
            int i;
            for (i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg == "--")
                {
                    // consume "--" and stop parsing
                    i++;
                    break;
                }

                if (arg.StartsWith("-"))
                {
                    arg = arg.Substring(1);
                    if (arg.StartsWith("-"))
                    {
                        arg = arg.Substring(1);
                    }
                    if (arg.StartsWith("-"))
                    {
                        return false; // error
                    }

                    // from here there are two things that can happen
                    // the parameter can be a boolean flag in which case
                    // the flag is assumed to be toggled "on" when specified 
                    // without an argument this is different from all other
                    // options

                    string k, v; TypeCode t;

                    var split = arg.IndexOf('=');
                    if (split == -1)
                    {
                        k = arg;
                        v = null; // if non-boolean option then set elsewhere
                    }
                    else
                    {
                        k = arg.Substring(0, split);
                        v = arg.Substring(split + 1);
                    }

                    var formalIndex = Array.IndexOf(_formal, k);
                    if (formalIndex == -1)
                    {
                        Console.Error.WriteLine($"Error: option '{arg}' is invalid");
                        foreach (var maybe in _formal.OrderBy(x => LevenshteinDistance(x, k)).Take(3))
                        {
                            Console.Error.WriteLine($"Error: did you mean '--{maybe}'?");
                        }
                        return false;
                    }
                    else
                    {
                        t = _type[formalIndex];
                    }

                    if (v == null)
                    {
                        if (t == TypeCode.Boolean)
                        {
                            v = Boolean.TrueString; // edge case: boolean option defaults to true when specified
                        }
                        else
                        {
                            var j = i + 1;
                            if (j < args.Length)
                            {
                                v = args[j]; // if v starts with hypen (-) it could be a user error but we can't really tell possibly generate a warning
                                i = j;
                            }
                        }
                    }

                    if (v == null) // empty string is not null!
                    {
                        Console.Error.WriteLine($"Error: option '{arg}' is missing additional required argument");
                        return false;
                    }

                    // todo: check type

                    Set(k, v);
                    continue;
                }

                // done
                break;
            }

            Remainder = Slice(args, i);
            return true;
        }

        private readonly Dictionary<string, List<string>> _parsed = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public bool Has(string option)
        {
            return _parsed.ContainsKey(option);
        }

        public void Set(string option, string value)
        {
            List<string> list;
            if (_parsed.TryGetValue(option, out list))
            {
                list.Add(value);
            }
            else
            {
                _parsed.Add(option, new List<string> { value });
            }
        }

        public IEnumerable<string> Get(string option)
        {
            List<string> list;
            if (_parsed.TryGetValue(option, out list))
            {
                return list;
            }
            return Enumerable.Empty<string>();
        }

        public string GetSingle(string option) => Get(option).Single();
        public string GetSingleOrDefault(string option, string defaultValue = null) => Get(option).SingleOrDefault() ?? defaultValue;

        public IEnumerable<T> Get<T>(string option)
            where T : struct, IConvertible
        {
            List<string> list;
            if (_parsed.TryGetValue(option, out list))
            {
                return list.Select(v => (T)Convert.ChangeType(v, typeof(T), CultureInfo.InvariantCulture));
            }
            return Enumerable.Empty<T>();
        }

        public T GetSingle<T>(string option) where T : struct, IConvertible => Get<T>(option).Single();
        public T GetSingleOrDefault<T>(string option, T defaultValue = default(T)) where T : struct, IConvertible => Get<T>(option).Any() ? Get<T>(option).Single() : defaultValue;

        private static int LevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
