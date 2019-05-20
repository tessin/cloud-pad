using CloudPad.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using Tessin;

namespace CloudPad {
  public static class Program {
    public static Task MainAsync(object userQuery, string[] args) {
      if (userQuery == null) {
        throw new ArgumentNullException("User query cannot be null. You should pass 'this' here.", nameof(userQuery));
      }

      args = args ?? new string[0]; // note: `args` can be null

      var currentQueryPath = Util.CurrentQueryPath;
      if (currentQueryPath == null) {
        throw new InvalidOperationException("A file name is required (save your LINQPad query to disk). Without it, we cannot establish a context for your functions.");
      }

      // when executed as a function this main is never called
      if (args.Length == 0) {
        // default behavior, i.e. development mode

        var tcs = new TaskCompletionSource<int>();

        Util.Cleanup += (sender, e) => {
          tcs.SetCanceled();
        };

        // we need to launch the job host to get a working directory to compile the script into

        // todo: storage emulator?

#if DEBUG
        var jobHost = JobHost.Launch(currentQueryPath, @"C:\Users\leidegre\Source\tessin\cloud-pad3\CloudPad.FunctionApp\bin\Debug\net461");
#else
        var jobHost = JobHost.Launch(currentQueryPath);
#endif

        Compiler.Compile(new UserQueryInfo(userQuery), new CompilationOptions(currentQueryPath) {
          OutDir = jobHost.WorkingDirectory
        });

        // and we are live!

        return tcs.Task; // keep running...
      } else {
        var options = CommandLine.Parse(args, new Options { });
        if (options.compile) {
          var uqi = new UserQueryInfo(userQuery);

          var compilationOptions = new CompilationOptions(currentQueryPath);

          compilationOptions.OutDir = options.compile_out_dir == null ? Path.Combine(compilationOptions.QueryDirectoryName, uqi.AssemblyName) : Path.GetFullPath(options.compile_out_dir);

          Compiler.Compile(new UserQueryInfo(uqi), compilationOptions);

          return Task.FromResult(0);
        }

        return Task.FromResult(1);
      }
    }
  }
}