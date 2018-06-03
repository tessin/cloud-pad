using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

namespace CloudPad
{
    public static class TimerFunctionEntryPoint
    {
        public static Task Run(
            TimerInfo myTimer,
            System.Threading.CancellationToken cancellationToken,
            ExecutionContext executionContext,
            TraceWriter log
        )
        {
            return FunctionExecutor.Current.RunTimerTriggerAsync(
                executionContext,
                new FunctionTraceWriter(log),
                cancellationToken
            );
        }
    }
}
