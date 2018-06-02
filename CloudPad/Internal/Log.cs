using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CloudPad.Internal
{
    static class Log
    {
        private static readonly int _currentProcessId = Process.GetCurrentProcess().Id;

        public static class Trace
        {
            [Conditional("TRACE")]
            public static void Append(FormattableString formattable, Guid? correlationId = null, [CallerMemberName] string callerMemberName = null)
            {
                Append(FormattableString.Invariant(formattable), correlationId, callerMemberName);
            }

            [Conditional("TRACE")]
            public static void Append(string message, Guid? correlationId = null, [CallerMemberName] string callerMemberName = null)
            {
                if (correlationId.HasValue)
                {
                    System.Diagnostics.Trace.WriteLine(
                        FormattableString.Invariant(
                            $"{DateTimeOffset.Now:o} [{_currentProcessId,5}] {correlationId,36} {callerMemberName}: {message?.Trim()}"
                        )
                    );
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine(
                        FormattableString.Invariant(
                            $"{DateTimeOffset.Now:o} [{_currentProcessId,5}] {callerMemberName}: {message?.Trim()}"
                        )
                    );
                }
            }
        }

        public static class Debug
        {
            [Conditional("DEBUG")]
            public static void Append(FormattableString formattable, Guid? correlationId = null, [CallerMemberName] string callerMemberName = null)
            {
                Append(FormattableString.Invariant(formattable), correlationId, callerMemberName);
            }

            [Conditional("DEBUG")]
            public static void Append(string message, Guid? correlationId = null, [CallerMemberName] string callerMemberName = null)
            {
                if (correlationId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine(
                        FormattableString.Invariant(
                            $"{DateTimeOffset.Now:o} [{_currentProcessId,5}] {correlationId,36} {callerMemberName}: {message?.Trim()}"
                        )
                    );
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        FormattableString.Invariant(
                            $"{DateTimeOffset.Now:o} [{_currentProcessId,5}] {callerMemberName}: {message?.Trim()}"
                        )
                    );
                }
            }
        }
    }
}
