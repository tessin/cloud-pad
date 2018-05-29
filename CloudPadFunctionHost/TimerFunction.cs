using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

namespace CloudPadFunctionHost
{
    public static class TimerFunction
    {
        [FunctionName(nameof(TimerFunction))]
        public static Task Run(
            [TimerTrigger("")]
            TimerInfo myTimer,
            System.Threading.CancellationToken cancellationToken,
            ExecutionContext executionContext,
            TraceWriter log
        )
        {
            return FunctionExecutor.Current.RunTimerTriggerAsync(executionContext, cancellationToken);
        }
    }
}
