using System.ComponentModel;
using System.Threading.Tasks;

namespace CloudPad
{
    public static class CloudPad
    {
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task MainAsync(this object context, string[] args)
        {
            using (var host = new CloudPadJobHost(context, args))
            {
                await host.WaitAsync();
            }
        }
    }
}
