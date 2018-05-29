using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CloudPad
{
    static class DebugLog
    {
        private static readonly int _currentProcessId = Process.GetCurrentProcess().Id;

        static DebugLog()
        {
            //Debugger.Launch();

            //var extensions = Type.GetType("LINQPad.Extensions, LINQPad", false);
            //if (extensions != null)
            //{

            //}
        }

        [Conditional("DEBUG")]
        public static void Append(FormattableString formattable, [CallerMemberName] string callerMemberName = null)
        {
            Append(FormattableString.Invariant(formattable), callerMemberName);
        }

        [Conditional("DEBUG")]
        public static void Append(string message, [CallerMemberName] string callerMemberName = null)
        {
            Debug.WriteLine(FormattableString.Invariant($"{DateTimeOffset.Now:o} [{_currentProcessId,5}] {callerMemberName,16} {message}"));
        }
    }
}
