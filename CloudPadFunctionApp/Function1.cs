using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPadFunctionApp
{
    public static class Function1
    {
        // these proxies all look the same, there's internal routing in the LINQPad query

        private static readonly Invoker Invoker = new Invoker();

        [FunctionName("Function1")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello")]
            HttpRequestMessage req,
            CancellationToken cancellationToken,
            Microsoft.Azure.WebJobs.ExecutionContext executionContext,
            TraceWriter log
        )
        {
            var functionJsonFn = Path.Combine(executionContext.FunctionDirectory, "function.json");

            // the actual parameters are read from the functionJsonFn file
            var linqPadScriptFileName = @"C:\Users\leidegre\Source\tessin\CloudPad\CloudPadConsoleApp\server.linq";
            var methodName = "Hello";

            return await Invoker.RunHttpTriggerAsync(linqPadScriptFileName, methodName, req, cancellationToken);
        }
    }
}
