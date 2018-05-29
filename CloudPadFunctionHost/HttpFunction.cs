using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPadFunctionHost
{
    public static class HttpFunction
    {
        [FunctionName(nameof(HttpFunction))]
        public static Task<HttpResponseMessage> Run(
            [HttpTrigger(Route = "")]
            HttpRequestMessage req,
            System.Threading.CancellationToken cancellationToken,
            ExecutionContext executionContext,
            TraceWriter log
        )
        {
            return FunctionExecutor.Current.RunHttpTriggerAsync(executionContext, req, cancellationToken);
        }
    }
}
