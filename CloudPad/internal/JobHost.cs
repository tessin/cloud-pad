using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace CloudPad.Internal {
  class JobHost {
    private static string GetAzureFunctionsCoreTools(string version) {
      var funcRoot = Path.Combine(Env.GetProgramDataDirectory(), "func");
      var funcDir = Path.Combine(funcRoot, version);
      var funcFileName = Path.Combine(funcDir, "func.exe");
      if (!File.Exists(funcFileName)) {
        Directory.CreateDirectory(funcDir);
        var azureFunctionsCliZip = funcDir + ".zip";
        var req = WebRequest.Create($"https://github.com/tessin/cloud-pad/releases/download/2.0.0/Azure.Functions.Cli.{version}-net461.zip");
        using (var res = req.GetResponse()) {
          using (var zip = File.Create(azureFunctionsCliZip)) {
            res.GetResponseStream().CopyTo(zip);
          }
        }
        ZipFile.ExtractToDirectory(azureFunctionsCliZip, funcDir);
        File.Delete(azureFunctionsCliZip);
      }
      return funcDir;
    }

    public static async Task LaunchAsync(string functionAppDirectory) {
      // StartHostAction.RunAsync
      // https://github.com/Azure/azure-functions-core-tools/blob/1.0.19/src/Azure.Functions.Cli/Actions/HostActions/StartHostAction.cs#L102-L143

      // ================

      var azureFunctionsCoreTools = GetAzureFunctionsCoreTools("1.0.19");

      // ================

      var assemblyBindings = AssemblyBindingConfig.LoadFrom(Path.Combine(azureFunctionsCoreTools, "func.exe.config"));

      AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
        Debug.WriteLine($"AssemblyResolve '{e.Name}'", "func.exe");

        var name = new AssemblyName(e.Name);

        var probePaths = new[] {
            Path.Combine(azureFunctionsCoreTools, name.Name + ".dll"), // DLL first
            Path.Combine(azureFunctionsCoreTools, name.Name + ".exe"),
          };

        foreach (var probePath in probePaths) {
          if (File.Exists(probePath)) {
            var probeName = AssemblyName.GetAssemblyName(probePath);

            // look for redirect
            var bindingRedirect = assemblyBindings.Find(probeName);
            if (bindingRedirect != null) {
              if (bindingRedirect.NewVersion == probeName.Version) {
                Debug.WriteLine($"ResolvedAssembly '{e.Name}'", "func.exe");
                return Assembly.LoadFrom(probePath);
              }
            }

            if (probeName.FullName == e.Name) {
              Debug.WriteLine($"ResolvedAssembly '{e.Name}'", "func.exe");
              return Assembly.LoadFrom(probePath);
            }
          }
        }

        Debug.WriteLine($"UnresolvedAssembly '{e.Name}'", "func.exe");
        return null;
      };

      // ================

      var funcAssembly = Assembly.LoadFrom(Path.Combine(azureFunctionsCoreTools, "func.exe"));

      Debug.WriteLine("new StartHostAction();", "func.exe");
      var startHostAction = funcAssembly.GetType("Azure.Functions.Cli.Actions.HostActions.StartHostAction");
      var action = Activator.CreateInstance(startHostAction, new object[] { null });

      // Utilities.PrintLogo(); // skip this, the log is spammy enough as it is

      Debug.WriteLine("var scriptPath = ScriptHostHelpers.GetFunctionAppRootDirectory(...);", "func.exe");
      var scriptHostHelpers = funcAssembly.GetType("Azure.Functions.Cli.Helpers.ScriptHostHelpers");
      var getFunctionAppRootDirectory = scriptHostHelpers.GetMethod("GetFunctionAppRootDirectory", BindingFlags.Public | BindingFlags.Static);
      var scriptPath = getFunctionAppRootDirectory.Invoke(null, new object[] { functionAppDirectory });

      Debug.WriteLine("var traceLevel = await ScriptHostHelpers.GetTraceLevel(scriptPath);", "func.exe");
      var getTraceLevel = scriptHostHelpers.GetMethod("GetTraceLevel", BindingFlags.NonPublic | BindingFlags.Static);
      var traceLevelTask = (Task)getTraceLevel.Invoke(null, new object[] { scriptPath });
      await traceLevelTask;
      var traceLevel = GetTaskResult(traceLevelTask);

      Debug.WriteLine("var settings = SelfHostWebHostSettingsFactory.Create(traceLevel, scriptPath);", "func.exe");
      var selfHostWebHostSettingsFactory = funcAssembly.GetType("Azure.Functions.Cli.Common.SelfHostWebHostSettingsFactory");
      var create = selfHostWebHostSettingsFactory.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
      var settings = create.Invoke(null, new object[] { traceLevel, scriptPath });

      // Setup(); // skip
      var baseAddress = new Uri("http://localhost:7071/");

      // hardcoded to use Environment.CurrentDirectory
      // ReadSecrets(scriptPath, baseAddress);  // skip

      var selfHostAssembly = Assembly.LoadFrom(Path.Combine(azureFunctionsCoreTools, "System.Web.Http.SelfHost.dll"));

      Debug.WriteLine("var config = new HttpSelfHostConfiguration(baseAddress);", "func.exe");
      var httpSelfHostConfiguration = selfHostAssembly.GetType("System.Web.Http.SelfHost.HttpSelfHostConfiguration");
      var config = Activator.CreateInstance(httpSelfHostConfiguration, new object[] { baseAddress });
      httpSelfHostConfiguration.GetProperty("IncludeErrorDetailPolicy").SetValue(config, 2); // Always
      httpSelfHostConfiguration.GetProperty("TransferMode").SetValue(config, 1); // Streamed
      httpSelfHostConfiguration.GetProperty("HostNameComparisonMode").SetValue(config, 1); // Exact
      httpSelfHostConfiguration.GetProperty("MaxReceivedMessageSize").SetValue(config, 104857600L);

      Debug.WriteLine("config.Formatters.Add(new JsonMediaTypeFormatter());", "func.exe");
      var formatting = Assembly.LoadFrom(Path.Combine(azureFunctionsCoreTools, "System.Net.Http.Formatting.dll"));
      var jsonMediaTypeFormatter = Activator.CreateInstance(formatting.GetType("System.Net.Http.Formatting.JsonMediaTypeFormatter"));
      var formatters = httpSelfHostConfiguration.GetProperty("Formatters").GetValue(config);
      var add = formatters.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
      add.Invoke(formatters, new object[] { jsonMediaTypeFormatter });

      // ???
      // todo: Environment.SetEnvironmentVariable("EDGE_NODE_PARAMS", $"--debug={NodeDebugPort}", EnvironmentVariableTarget.Process);

      Debug.WriteLine("WebApiConfig.Initialize(config, settings: settings);", "func.exe");
      var webHostAssembly = Assembly.LoadFrom(Path.Combine(azureFunctionsCoreTools, "Microsoft.Azure.WebJobs.Script.WebHost.dll"));
      var webApiConfig = webHostAssembly.GetType("Microsoft.Azure.WebJobs.Script.WebHost.WebApiConfig");
      var initialize = webApiConfig.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
      initialize.Invoke(null, new object[] { config, null, settings, null });

      Debug.WriteLine("using (var httpServer = new HttpSelfHostServer(config))", "func.exe");
      var httpSelfHostServer = selfHostAssembly.GetType("System.Web.Http.SelfHost.HttpSelfHostServer");
      var server = (IDisposable)Activator.CreateInstance(httpSelfHostServer, new object[] { config });
      using (server) {
        Debug.WriteLine("await httpServer.OpenAsync();", "func.exe");
        var openAsync = httpSelfHostServer.GetMethod("OpenAsync", BindingFlags.Public | BindingFlags.Instance);
        await (Task)openAsync.Invoke(server, null);

        Trace.WriteLine($"Listening on {baseAddress}");

        Debug.WriteLine("await PostHostStartActions(config);", "func.exe");
        var postHostStartActions = startHostAction.GetMethod("PostHostStartActions", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)postHostStartActions.Invoke(action, new object[] { config });

        var tcs = new TaskCompletionSource<int>();

        Util.Cleanup += async (sender, e) => {
          Debug.WriteLine("await httpServer.CloseAsync();", "func.exe");
          var closeAsync = httpSelfHostServer.GetMethod("CloseAsync", BindingFlags.Public | BindingFlags.Instance);
          await (Task)closeAsync.Invoke(server, null);
          tcs.SetResult(0);
        };

        await tcs.Task; // hang
      }
    }

    // utils

    private static object GetTaskResult(Task task) {
      return task.GetType().GetProperty("Result").GetValue(task);
    }
  }
}
