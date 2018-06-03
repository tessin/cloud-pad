using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPad
{
    public static class HttpFunctionEntryPoint
    {
        public static Task<HttpResponseMessage> Run(
            HttpRequestMessage req,
            System.Threading.CancellationToken cancellationToken,
            ExecutionContext executionContext,
            TraceWriter log
        )
        {
            return FunctionExecutor.Current.RunHttpTriggerAsync(
                executionContext,
                req,
                new FunctionTraceWriter(log),
                cancellationToken
            );
        }
    }
}
