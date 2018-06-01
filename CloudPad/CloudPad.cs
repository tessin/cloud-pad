using CloudPad.Internal;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CloudPad
{
    public static class CloudPad
    {
        public static async Task MainAsync(object context, string[] args)
        {
            using (var host = new JobHost(context, args))
            {
                await host.WaitAsync();
            }
        }
    }
}
