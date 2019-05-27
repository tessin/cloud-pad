using CloudPad.Internal;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Net;
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
      arguments.AddArgument(typeof(ITraceWriter), new TraceWriterWrapper(log));

      var cloudStorageHelperType = typeof(CloudStorageHelper);
      if (func.Function.ParameterBindings.HasBinding(cloudStorageHelperType)) {
        arguments.AddArgument(cloudStorageHelperType, new CloudStorageHelper(CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"))));
      }

      object result;
      try {
        result = await func.InvokeAsync(arguments, log);
      } catch (ArgumentException ex) {
        // special sauce for HTTP trigger
        log.Error(ex.Message, ex);
        return req.CreateResponse(HttpStatusCode.BadRequest, new { ok = false, message = ex.Message });
      } catch {
        throw;
      }

      var taskWithValue = result as Task<HttpResponseMessage>;
      if (taskWithValue != null) {
        return await taskWithValue; // unwrap async http response
      }

      var value = result as HttpResponseMessage;
      if (value != null) {
        return value; // return synchronous http response
      }

      return req.CreateResponse(HttpStatusCode.NoContent);
    }
  }
}