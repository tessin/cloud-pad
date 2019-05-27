using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace CloudPad.FunctionApp
{
    public static class QueueTrigger
    {
        public static async Task Run(CloudQueueMessage msg,
          System.Threading.CancellationToken cancellationToken,
          ExecutionContext executionContext, // note that this name is also found in namespace "System.Threading" we don't want that
          TraceWriter log)
        {

            var func = await FunctionExecutor.GetAndInvalidateAsync(executionContext.FunctionDirectory, log);

            var arguments = new FunctionArgumentList();

            // queue message type
            var parameters = func.Function.Method.GetParameters();
            var parameter0Type = parameters[0].ParameterType;
            if (parameter0Type != typeof(CloudQueueMessage))
            {
                arguments.AddArgument(parameter0Type, JsonConvert.DeserializeObject(msg.AsString, parameter0Type));
            }

            arguments.AddArgument(typeof(CloudQueueMessage), msg);

            // common
            arguments.AddArgument(typeof(System.Threading.CancellationToken), cancellationToken);
            arguments.AddArgument(typeof(ITraceWriter), new TraceWriterWrapper(log));

            var cloudStorageHelperType = typeof(ICloudStorage);
            if (func.Function.ParameterBindings.HasBinding(cloudStorageHelperType))
            {
                arguments.AddArgument(cloudStorageHelperType, new CloudStorage(CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"))));
            }

            await func.InvokeAsync(arguments, log);
        }
    }
}
