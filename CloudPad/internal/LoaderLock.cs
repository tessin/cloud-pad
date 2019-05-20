using System;
using System.Diagnostics;
using System.Threading;

namespace CloudPad.Internal {
  // this lock protects the application base from inprop access during user query construction
  // it's necessary due to the way C# has been design as well as how LINQPad works
  class LoaderLock : IDisposable {
    private static readonly object _lockObject = new object();

    public static bool IsHeld() {
      return Monitor.IsEntered(_lockObject);
    }

    [Conditional("DEBUG")]
    public static void AssertIsHeld() {
      if (!IsHeld()) {
        if (Util.CurrentQuery == null) {
          throw new InvalidOperationException("Loader lock not held");
        }
      }
    }

    public static string _applicationBase;

    /// <summary>
    /// This value is always null when running from within LINQPad. When running outside LINQPad the value is only valid for the duration of the LINQPad UserQuery constructor.
    /// </summary>
    public static string ApplicationBase {
      get {
        AssertIsHeld();
        return _applicationBase;
      }
    }

    private bool _lockTaken;

    public LoaderLock(string applicationBase) {
      Monitor.Enter(_lockObject, ref _lockTaken);
      if (_lockTaken) {
        _applicationBase = applicationBase;
      }
    }

    public void Dispose() {
      if (_lockTaken) {
        _applicationBase = null;
        Monitor.Exit(_lockObject);
        _lockTaken = false; // prevent accidental exit twice
      }
    }
  }
}
