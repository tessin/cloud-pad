using CloudPad.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("CloudPadTest")]

namespace CloudPad
{
  public static class Program
  {
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    public static async Task MainAsync(object context, string[] args)
    {
      var hasConsoleWindow = Environment.UserInteractive && !(GetConsoleWindow() == IntPtr.Zero);

      if (hasConsoleWindow)
      {
        Trace.Listeners.Add(new ConsoleTraceListener());
      }

      using (var host = new JobHost(context, args ?? new string[0]))
      {
        try
        {
          await host.WaitAsync();
        }
        catch (Exception ex)
        {
          if (0 < args?.Length)
          {
            if (hasConsoleWindow)
            {
              Console.WriteLine(FormattableString.Invariant($"{ex.GetType().FullName}: {ex.Message}"));

              var stackTrace = ex.StackTrace;
              if (!string.IsNullOrEmpty(stackTrace))
              {
                var reader = new StringReader(stackTrace);
                for (; ; )
                {
                  var ln = reader.ReadLine()?.Trim();
                  if (ln == null)
                  {
                    break;
                  }
                  if (0 < ln.Length)
                  {
                    Console.WriteLine("  " + ln);
                  }
                }
              }

              Console.WriteLine();
              Console.WriteLine("Press enter key to continue . . .");
              Console.ReadLine();

              return; // don't re-throw 
            }
          }
          throw;
        }
      }
    }
  }
}
