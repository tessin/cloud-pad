using CloudPad.Internal;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("CloudPadTest")]

namespace CloudPad
{
    public static class Program
    {
        public static async Task MainAsync(object context, string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            using (var host = new JobHost(context, args))
            {
                await host.WaitAsync();
            }
        }
    }
}
