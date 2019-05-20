using System;
using System.Linq;
using System.Reflection;

namespace CloudPad {
  /// <summary>
  /// Late bound subset of LINQPad.Util interface. These utilities short circuit gracefully (do nothing) when not running within a LINQPad context.
  /// </summary>
  static class Util {
    private static readonly Type _util;

    private static readonly EventInfo _cleanup;
    public static event EventHandler Cleanup {
      add {
        _cleanup?.AddEventHandler(null, value);
      }
      remove {
        _cleanup?.RemoveEventHandler(null, value);
      }
    }

    private static readonly PropertyInfo _currentQuery;
    public static object CurrentQuery {
      get {
        return _currentQuery?.GetValue(null);
      }
    }

    private static readonly PropertyInfo _currentQueryPath;
    public static string CurrentQueryPath {
      get {
        return (string)_currentQueryPath?.GetValue(null);
      }
    }

    static Util() {
      _util = Type.GetType("LINQPad.Util, LINQPad", false, false);

      _cleanup = _util?.GetEvent("Cleanup");

      _currentQuery = _util?.GetProperty("CurrentQuery");
      _currentQueryPath = _util?.GetProperty("CurrentQueryPath");

      _cache = _util?.GetMember("Cache")
        .OfType<MethodInfo>()
        .First(m => m.IsGenericMethod && m.GetParameters().Length == 2)
        .MakeGenericMethod(typeof(object));
    }

    private static readonly MethodInfo _cache;
    public static object Cache(Func<object> dataFactory, string key) {
      return _cache.Invoke(null, new object[] { dataFactory, key });
    }
  }

  /// <summary>
  /// Late bound subset of LINQPad.Extensions interface. These utilities short circuit gracefully (do nothing) when not running within a LINQPad context.
  /// </summary>
  static class Extensions {
    private static readonly Type _extensions;

    private static readonly MethodInfo _dump;

    static Extensions() {
      _extensions = Type.GetType("LINQPad.Extensions, LINQPad", false, false);

      // see https://blogs.msdn.microsoft.com/yirutang/2005/09/14/getmethod-limitation-regarding-generics/

      _dump = _extensions?.GetMember("Dump")
        .OfType<MethodInfo>()
        .First(m => m.GetParameters().Length == 1)
        .MakeGenericMethod(typeof(object));
    }

    public static void Dump(object value) {
      _dump?.Invoke(null, new object[] { value });
    }
  }
}