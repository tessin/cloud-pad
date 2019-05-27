using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;

namespace CloudPad.FunctionApp
{
    public static class BlobTrigger
    {
        public static async Task Run(
          CloudBlockBlob blob,
          System.Threading.CancellationToken cancellationToken,
          ExecutionContext executionContext, // note that this name is also found in namespace "System.Threading" we don't want that
          TraceWriter log)
        {
            var func = await FunctionExecutor.GetAndInvalidateAsync(executionContext.FunctionDirectory, log);

            var arguments = new FunctionArgumentList();

            arguments.AddArgument(typeof(CloudBlockBlob), blob);
            arguments.AddArgument(typeof(System.Threading.CancellationToken), cancellationToken);
            arguments.AddArgument(typeof(ExecutionContext), executionContext);
            arguments.AddArgument(typeof(TraceWriter), log);
            arguments.AddArgument(typeof(ITraceWriter), new TraceWriterWrapper(log));

            await func.InvokeAsync(arguments, log);
        }
    }
}