using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPadFunctionHost
{
    public static class HttpFunction
    {
        public static Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, null, Route = "")]
            HttpRequestMessage req,
            System.Threading.CancellationToken cancellationToken,
            ExecutionContext executionContext,
            TraceWriter log
        )
        {
            using (new TraceWriterListener(log))
            {
                return FunctionExecutor.Current.RunHttpTriggerAsync(executionContext, req, cancellationToken);
            }
        }
    }
}
