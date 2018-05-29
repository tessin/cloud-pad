using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
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
                var details2 = JsonConvert.DeserializeObject<FunctionExecutorDetails>(File.ReadAllText(fn));
                details2.LINQPadScriptFileName = Path.GetFullPath(Path.Combine(executionContext.FunctionDirectory, details2.LINQPadScriptFileName)); // resolve path
                _functionDetails.TryAdd(fn, details = details2);
            }
            Debug.WriteLine($"{executionContext.InvocationId} -> {details.LINQPadScriptFileName}:{details.LINQPadScriptMethodName}");
            return details;
        }

        public Task<HttpResponseMessage> RunHttpTriggerAsync(ExecutionContext executionContext, HttpRequestMessage req, System.Threading.CancellationToken cancellationToken)
        {
            var details = Resolve(executionContext);

            return _invoker.RunHttpTriggerAsync(
                details.LINQPadScriptFileName,
                details.LINQPadScriptMethodName,
                req,
                cancellationToken
            );
        }

        public Task RunTimerTriggerAsync(ExecutionContext executionContext, System.Threading.CancellationToken cancellationToken)
        {
            var details = Resolve(executionContext);

            return _invoker.RunTimerTriggerAsync(
                details.LINQPadScriptFileName,
                details.LINQPadScriptMethodName,
                cancellationToken
            );
        }
    }
}
