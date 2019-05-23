using CloudPad.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessin;

namespace CloudPad {
  public static class Program {
    static Program() {
#if _DEBUG
      var d = AppDomain.CurrentDomain;
      var xs = new System.Collections.Generic.HashSet<string>();
      foreach (var loadedAssembly in d.GetAssemblies()) {
        Trace.WriteLine($"Assembly '{loadedAssembly.FullName}' loaded from location '{loadedAssembly.Location}'");
        xs.Add(loadedAssembly.FullName);
      }
      // see https://stackoverflow.com/a/14893126/58961
      //d.AssemblyLoad += (sender, e) => {
      //  Trace.WriteLine($"Assembly '{e.LoadedAssembly.FullName}' loaded from location '{e.LoadedAssembly.Location}'");
      //};
      System.Threading.ThreadPool.QueueUserWorkItem(_ => {
        for (; ; ) {
          foreach (var loadedAssembly in d.GetAssemblies()) {
            if (xs.Add(loadedAssembly.FullName)) {
              Trace.WriteLine($"*Assembly '{loadedAssembly.FullName}' loaded from location '{loadedAssembly.Location}'");
            }
          }
          System.Threading.Thread.Sleep(100);
        }
      });
#endif
    }

    // LINQPad script entry point 
    // when deployed as an Azure Function this method is not used
    public static async Task<int> MainAsync(object userQuery, string[] args) {
      var LPRun = false;
      if ("LPRun.exe".Equals(Process.GetCurrentProcess().MainModule.ModuleName, StringComparison.OrdinalIgnoreCase)) {
        LPRun = true;
        Trace.Listeners.Add(new ConsoleTraceListener());
      }

      if (userQuery == null) {
        throw new ArgumentNullException("User query cannot be null. You should pass 'this' here.", nameof(userQuery));
      }
      var userQueryInfo = new UserQueryTypeInfo(userQuery);

      var currentQuery = Util.CurrentQuery;
      if (currentQuery == null) {
        throw new InvalidOperationException("This script must be run from wthin a LINQPad context (either via LINQPad or LPRun).");
      }
      var currentQueryInfo = new QueryInfo(currentQuery);

      var currentQueryPath = Util.CurrentQueryPath;
      if (currentQueryPath == null) {
        throw new InvalidOperationException("A file name is required (save your LINQPad query to disk). Without it, we cannot establish a context for your functions.");
      }
      var currentQueryPathInfo = new QueryPathInfo(currentQueryPath);

      // ========

      args = args ?? new string[0]; // note: `args` can be null
      if (args.Length == 0) {
        // todo: storage emulator?

        var workingDirectory = Path.Combine(Env.GetLocalAppDataDirectory(), currentQueryPathInfo.InstanceId);

        FunctionApp.Deploy(workingDirectory);

        JobHost.Prepare();

        Compiler.Compile(new UserQueryTypeInfo(userQuery), currentQueryInfo, new CompilationOptions(currentQueryPath) {
          OutDir = workingDirectory
        }, currentQueryInfo);

        await JobHost.LaunchAsync(workingDirectory);

        return 0;
      } else {
        try {
          var options = CommandLine.Parse(args, new Options { });
          if (options.compile) {
            var compilationOptions = new CompilationOptions(currentQueryPath);
            compilationOptions.OutDir = options.out_dir == null ? Path.Combine(compilationOptions.QueryDirectoryName, compilationOptions.QueryName + "_" + userQueryInfo.Id) : Path.GetFullPath(options.out_dir);
            Compiler.Compile(userQueryInfo, currentQueryInfo, compilationOptions, currentQueryInfo);
            Trace.WriteLine($"Done. Output written to '{compilationOptions.OutDir}'");
            return 0;
          } else if (options.publish) {
            var compilationOptions = new CompilationOptions(currentQueryPath);
            compilationOptions.OutDir = Path.Combine(compilationOptions.QueryDirectoryName, compilationOptions.QueryName + "_" + userQueryInfo.Id + "_" + Environment.TickCount);
            try {
              Compiler.Compile(userQueryInfo, currentQueryInfo, compilationOptions, currentQueryInfo);
              var publishSettingsFileName = FileUtil.ResolveSearchPatternUpDirectoryTree(compilationOptions.QueryDirectoryName, "*.PublishSettings").Single();
              var kudu = KuduClient.FromPublishProfile(publishSettingsFileName);
              Trace.WriteLine($"Publishing to '{kudu.Host}'...");
              kudu.ZipUpload(compilationOptions.OutDir);
            } finally {
              if (Directory.Exists(compilationOptions.OutDir)) {
                Directory.Delete(compilationOptions.OutDir, true);
              }
            }
            Trace.WriteLine("Done.");
            return 0;
          } else if (options.prepare) {
            var compilationOptions = new CompilationOptions(currentQueryPath);
            compilationOptions.OutDir = options.out_dir == null ? Path.Combine(compilationOptions.QueryDirectoryName, compilationOptions.QueryName + "_publish") : Path.GetFullPath(options.out_dir);
            FunctionApp.Deploy(compilationOptions.OutDir);
            Compiler.Compile(userQueryInfo, currentQueryInfo, compilationOptions, currentQueryInfo);
            Trace.WriteLine($"Done. Output written to '{compilationOptions.OutDir}'");
            return 0;
          }
        } catch (Exception ex) {
          if (Environment.UserInteractive) {
            if (LPRun) {
              Console.WriteLine(ex.Message);
              Console.WriteLine("Press any key to continue...");
              Console.ReadKey();
            }
          }
          throw;
        }
        return 1;
      }
    }
  }
}