using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tessin {
  class Options {
    public bool compile;
    public string compile_out_dir;
  }

  static class CommandLine {
    public static T Parse<T>(IEnumerable<string> args, T options) {
      var type = typeof(T);

      var argv = args.GetEnumerator();
      while (argv.MoveNext()) {
        var option = argv.Current;
        if (option.StartsWith("-")) {
          option = option.Substring(1);
          if (option.StartsWith("-")) {
            option = option.Substring(1);
            if (option.StartsWith("-")) {
              throw new ArgumentException("cannot parse command line arguments");
            }
          }

          var f = type.GetField(option.Replace("-", "_"), BindingFlags.Public | BindingFlags.Instance);
          if (f == null) {
            throw new ArgumentException($"unknown command line option '{option}'");
          }

          if (f.FieldType == typeof(bool)) {
            f.SetValue(options, true);
            continue;
          }

          if (argv.MoveNext()) {
            if (f.FieldType == typeof(string)) {
              f.SetValue(options, argv.Current);
            } else {
              f.SetValue(options, Convert.ChangeType(argv.Current, f.FieldType, System.Globalization.CultureInfo.InvariantCulture));
            }
          } else {
            throw new ArgumentException($"command line option '{option}' is missing require argument");
          }
        }
      }

      return options;
    }
  }
}
