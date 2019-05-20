using CloudPad.Internal;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CloudPad.FunctionApp {
  class FunctionExecutor {
    static FunctionExecutor() {
      System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(LINQPad.Util).TypeHandle);
    }

    private static Dictionary<string, TaskCompletionSource<FunctionExecutor>> _d = new Dictionary<string, TaskCompletionSource<FunctionExecutor>>(StringComparer.OrdinalIgnoreCase);

    public static async Task<FunctionExecutor> GetAndInvalidateAsync(string functionDir, TraceWriter log) {
      var functionJsonFileName = Path.Combine(functionDir, "function.json");

      var func = await GetAsync(functionDir, functionJsonFileName, log);

      // Invalidate file dependency
      // (this isn't perfect but it only needs to work for the 1 person debugging/testing the app)

      var functionJsonLastWriteTime = File.GetLastWriteTimeUtc(functionJsonFileName);
      if (func.LastWriteTime < functionJsonLastWriteTime) {
        lock (_d) {
          _d.Remove(functionJsonFileName);
        }
        return await GetAsync(functionDir, functionJsonFileName, log);
      }

      return func;
    }

    private static Task<FunctionExecutor> GetAsync(string functionDir, string functionJsonFileName, TraceWriter log) {
      TaskCompletionSource<FunctionExecutor> tcs;

      lock (_d) {
        if (_d.TryGetValue(functionJsonFileName, out tcs)) {
          return tcs.Task;
        }
        _d.Add(functionJsonFileName, tcs = new TaskCompletionSource<FunctionExecutor>());
      }

      try {
        var functionJsonLastWriteTime = File.GetLastWriteTimeUtc(functionJsonFileName);
        var functionJson = JObject.Parse(File.ReadAllText(functionJsonFileName));

        var metadata = functionJson["cloudPad"];

        var applicationBase = (string)metadata["applicationBase"];
        var scriptFile = (string)metadata["scriptFile"];
        var typeName = (string)metadata["typeName"];
        var methodName = (string)metadata["methodName"];

        log.Info($"InitializingFunction ApplicationBase='{applicationBase}', ScriptFile='{scriptFile}'", nameof(FunctionExecutor));

        var fullPath = Path.GetFullPath(Path.Combine(functionDir, applicationBase, scriptFile));
        var dir = Path.GetDirectoryName(fullPath);

        log.Info($"ScriptFileLoad '{fullPath}'", nameof(FunctionExecutor));

        var assembly = Assembly.LoadFrom(fullPath);
        var type = assembly.GetType(typeName);
        var method = type.GetMethod(methodName);
        var func = FunctionBinder.Bind(method);

        // launch cleanup task

        tcs.SetResult(new FunctionExecutor(func, functionJsonLastWriteTime, dir));
      } catch (Exception ex) {
        tcs.SetException(ex);
      }

      return tcs.Task;
    }

    // ===

    public FunctionDescriptor Function { get; }
    public DateTime LastWriteTime { get; }
    public string ApplicationBase { get; }

    public FunctionExecutor(FunctionDescriptor function, DateTime lastWriteTime, string applicationBase) {
      Function = function;
      LastWriteTime = lastWriteTime;
      ApplicationBase = applicationBase;
    }

    public object Invoke(FunctionArgumentList arguments, TraceWriter log) {
      var m = Function.Method;

      object userQuery;

      using (new LoaderLock(ApplicationBase)) {
        // the user query constructor must run with a loader like lock
        // this is because there's no other way for us to inject context
        // in a reliable manner

        userQuery = Activator.CreateInstance(m.DeclaringType); // todo: connection 
      }

      return m.Invoke(userQuery, arguments.Apply(Function.ParameterBindings));
    }
  }
}