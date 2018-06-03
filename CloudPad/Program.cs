using CloudPad.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("CloudPadTest")]

namespace CloudPad
{
    public static class Program
    {
        public static async Task MainAsync(object context, string[] args)
        {
            int exitCode;

            using (var host = new JobHost(context, args ?? new string[0]))
            {
                exitCode = await host.WaitAsync();
            }

            if (exitCode != 0)
            {
                // this will bring down the LPRun hosting process 
                // when multiples queries share the same process 

                Environment.Exit(exitCode);
            }
        }
    }
}
