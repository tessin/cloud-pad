using CloudPad.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Tessin;

namespace CloudPad {
  public static class Program {
    // LINQPad script entry point 
    // when deployed as an Azure Function this method is not used
    public static async Task<int> MainAsync(object userQuery, string[] args) {
      if (userQuery == null) {
        throw new ArgumentNullException("User query cannot be null. You should pass 'this' here.", nameof(userQuery));
      }

      var userQueryInfo = new UserQueryTypeInfo(userQuery);

      args = args ?? new string[0]; // note: `args` can be null

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

      if (args.Length == 0) {
        // todo: storage emulator?

#if !DEBUG
        var workingDirectory = @"C:\Users\leidegre\Source\tessin\cloud-pad2\CloudPad.FunctionApp\bin\Debug\net461";
#else
        var workingDirectory = Path.Combine(Env.GetLocalAppDataDirectory(), currentQueryPathInfo.InstanceId);
#endif

        if (!File.Exists(Path.Combine(workingDirectory, "bin", "CloudPad.FunctionApp.dll"))) {
          // need to deploy the CloudPad.FunctionApp runtime
          using (var tempFile = new TempFile()) {
            var req = WebRequest.Create("https://github.com/tessin/cloud-pad/releases/download/2.0.0-beta.1/CloudPad.FunctionApp.zip");
            using (var res = req.GetResponse()) {
              using (var zip = File.Create(tempFile.FileName)) {
                res.GetResponseStream().CopyTo(zip);
              }
            }
            ZipFile.ExtractToDirectory(tempFile.FileName, workingDirectory);
          }
        }

        Compiler.Compile(new UserQueryTypeInfo(userQuery), new CompilationOptions(currentQueryPath) {
          OutDir = workingDirectory
        }, currentQueryInfo);

        await JobHost.LaunchAsync(workingDirectory);

        return 0;
      } else {
        if ("LPRun.exe".Equals(Process.GetCurrentProcess().MainModule.ModuleName, StringComparison.OrdinalIgnoreCase)) {
          Trace.Listeners.Add(new ConsoleTraceListener());
        }

        var options = CommandLine.Parse(args, new Options { });
        if (options.compile) {
          var compilationOptions = new CompilationOptions(currentQueryPath);
          compilationOptions.OutDir = options.out_dir == null ? Path.Combine(compilationOptions.QueryDirectoryName, compilationOptions.QueryName + "_" + userQueryInfo.Id) : Path.GetFullPath(options.out_dir);
          Compiler.Compile(userQueryInfo, compilationOptions, currentQueryInfo);
          Trace.WriteLine($"Done. Output written to '{compilationOptions.OutDir}'");
          return 0;
        } else if (options.publish) {
          var compilationOptions = new CompilationOptions(currentQueryPath);
          compilationOptions.OutDir = Path.Combine(compilationOptions.QueryDirectoryName, compilationOptions.QueryName + "_" + userQueryInfo.Id);
          try {
            Compiler.Compile(userQueryInfo, compilationOptions, currentQueryInfo);
            var publishSettingsFileName = FileUtil.ResolveSearchPatternUpDirectoryTree(compilationOptions.QueryDirectoryName, "*.PublishSettings").Single();
            var kudu = KuduClient.FromPublishProfile(publishSettingsFileName);
            Trace.WriteLine($"Publishing to '{kudu.Host}'...");
            kudu.ZipUpload(compilationOptions.OutDir);
          } finally {
            Directory.Delete(compilationOptions.OutDir, true);
          }
          Trace.WriteLine("Done.");
          return 0;
        }

        return 1;
      }
    }
  }
}