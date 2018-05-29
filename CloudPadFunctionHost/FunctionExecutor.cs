using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPadFunctionHost
{
    class FunctionExecutor
    {
        public static readonly FunctionExecutor Current = new FunctionExecutor();

        private Invoker _invoker;

        public FunctionExecutor()
        {
            _invoker = new Invoker();
        }

        private ConcurrentDictionary<string, FunctionExecutorDetails> _functionDetails = new ConcurrentDictionary<string, FunctionExecutorDetails>();

        public FunctionExecutorDetails Resolve(ExecutionContext executionContext)
        {
            var fn = Path.Combine(executionContext.FunctionDirectory, "function.json");
            if (!_functionDetails.TryGetValue(fn, out var details))
            {
                _functionDetails.TryAdd(fn, details = JsonConvert.DeserializeObject<FunctionExecutorDetails>(File.ReadAllText(fn)));
            }
            return details;
        }

        public Task<HttpResponseMessage> RunHttpTriggerAsync(ExecutionContext executionContext, HttpRequestMessage req, System.Threading.CancellationToken cancellationToken)
        {
            var details = Resolve(executionContext);

            return _invoker.RunHttpTriggerAsync(
                details.LINQPadScriptFileName,
                details.LINQPadMethodName,
                req,
                cancellationToken
            );
        }

        public Task RunTimerTriggerAsync(ExecutionContext executionContext, System.Threading.CancellationToken cancellationToken)
        {
            var details = Resolve(executionContext);

            return _invoker.RunTimerTriggerAsync(
                details.LINQPadScriptFileName,
                details.LINQPadMethodName,
                cancellationToken
            );
        }
    }
}
