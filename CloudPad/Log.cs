using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CloudPad
{
    internal static class Log
    {
        private static readonly int _currentProcessId = Process.GetCurrentProcess().Id;

        public static class Trace
        {
            [Conditional("TRACE")]
            public static void Append(FormattableString formattable, [CallerMemberName] string callerMemberName = null)
            {
                Append(FormattableString.Invariant(formattable), callerMemberName);
            }

            [Conditional("TRACE")]
            public static void Append(string message, [CallerMemberName] string callerMemberName = null)
            {
                System.Diagnostics.Trace.WriteLine(FormattableString.Invariant($"{DateTimeOffset.Now:o} [{_currentProcessId,5}] {callerMemberName,16} {message}"));
            }
        }

        public static class Debug
        {
            private static readonly int _currentProcessId = Process.GetCurrentProcess().Id;

            [Conditional("DEBUG")]
            public static void Append(FormattableString formattable, [CallerMemberName] string callerMemberName = null)
            {
                Append(FormattableString.Invariant(formattable), callerMemberName);
            }

            [Conditional("DEBUG")]
            public static void Append(string message, [CallerMemberName] string callerMemberName = null)
            {
                System.Diagnostics.Debug.WriteLine(FormattableString.Invariant($"{DateTimeOffset.Now:o} [{_currentProcessId,5}] {callerMemberName,16} {message}"));
            }
        }
    }
}
