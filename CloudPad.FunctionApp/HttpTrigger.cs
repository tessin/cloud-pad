using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPad.FunctionApp {
  public static class HttpTrigger {
    public static async Task<HttpResponseMessage> Run(
      HttpRequestMessage req,
      System.Threading.CancellationToken cancellationToken,
      ExecutionContext executionContext, // note that this name is also found in namespace "System.Threading" we don't want that
      TraceWriter log) {

      var func = await FunctionExecutor.GetAndInvalidateAsync(executionContext.FunctionDirectory, log);

      var arguments = new FunctionArgumentList();

      arguments.AddArgument(typeof(HttpRequestMessage), req);
      arguments.AddArgument(typeof(System.Threading.CancellationToken), cancellationToken);
      arguments.AddArgument(typeof(ExecutionContext), executionContext);
      arguments.AddArgument(typeof(TraceWriter), log);
      arguments.AddArgument(typeof(ILogger), new TraceWriterLogger(log));

      var result = func.Invoke(arguments, log);

      var taskWithValue = result as Task<HttpResponseMessage>;
      if (taskWithValue != null) {
        return await taskWithValue;
      }

      var task = result as Task;
      if (task != null) {
        await task;
      }

      return req.CreateResponse();
    }
  }
}