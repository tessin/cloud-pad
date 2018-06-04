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
            using (var host = new JobHost(context, args ?? new string[0]))
            {
                await host.WaitAsync();
            }
        }
    }
}
