using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Threading.Tasks;

namespace CloudPad.FunctionApp
{
    public static class TimerTrigger
    {
        public static async Task Run(TimerInfo timer,
            System.Threading.CancellationToken cancellationToken,
            ExecutionContext executionContext, // note that this name is also found in namespace "System.Threading" we don't want that
            TraceWriter log)
        {
            var func = await FunctionExecutor.GetAndInvalidateAsync(executionContext.FunctionDirectory, log);

            var arguments = new FunctionArgumentList();

            // common
            arguments.AddArgument(typeof(ITimerInfo), new TimerInfoWrapper(timer));
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
