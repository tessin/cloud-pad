using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace CloudPad.Internal {
  class JobHost {
    private static string GetAzureFunctionsCoreTools(string funcVersion) {
      var funcDir = Path.Combine(Env.GetLocalAppDataDirectory(), $"func.{funcVersion}");

      var funcFileName = Path.Combine(funcDir, "func.exe");
      if (!File.Exists(funcFileName)) {
        var azureFunctionsCliZip = funcDir + ".zip";
        var req = WebRequest.Create($"https://functionscdn.azureedge.net/public/{funcVersion}/Azure.Functions.Cli.zip");
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

      // The code does this based on the 1.0.19 release of the azure-functions-core-tools
      // We use reflection so that the CloudPad assembly (and NuGet package) doesn't depend on
      // the job host

      // the primary reasons are
      //  1. type ambiguity
      //  2. file size

      var azureFunctionsCoreTools = GetAzureFunctionsCoreTools("1.0.19");

      // ================

      AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
        Debug.WriteLine($"AssemblyResolve '{e.Name}'", "func.exe");

        var name = new AssemblyName(e.Name);

        var probePaths = new[] {
          Path.Combine(azureFunctionsCoreTools, name.Name + ".dll"), // DLL first
          Path.Combine(azureFunctionsCoreTools, name.Name + ".exe"),
        };

        foreach (var probePath in probePaths) {
          if (File.Exists(probePath)) {
            Debug.WriteLine($"ResolvedAssembly '{e.Name}' as '{probePath}'", "func.exe");

            return Assembly.LoadFrom(probePath);
          }
        }

        return null;
      };

      // ================

      var funcAssembly = Assembly.LoadFrom(Path.Combine(azureFunctionsCoreTools, "func.exe"));

      var startHostAction = funcAssembly.GetType("Azure.Functions.Cli.Actions.HostActions.StartHostAction");
      var action = Activator.CreateInstance(startHostAction, new object[] { null });

      // Utilities.PrintLogo(); // skip this, the log is spammy enough as it is

      var scriptHostHelpers = funcAssembly.GetType("Azure.Functions.Cli.Helpers.ScriptHostHelpers");
      var getFunctionAppRootDirectory = scriptHostHelpers.GetMethod("GetFunctionAppRootDirectory", BindingFlags.Public | BindingFlags.Static);
      var getTraceLevel = scriptHostHelpers.GetMethod("GetTraceLevel", BindingFlags.NonPublic | BindingFlags.Static);
      var scriptPath = getFunctionAppRootDirectory.Invoke(null, new object[] { functionAppDirectory });
      var traceLevelTask = (Task)getTraceLevel.Invoke(null, new object[] { scriptPath });
      await traceLevelTask;
      var traceLevel = GetTaskResult(traceLevelTask);

      var selfHostWebHostSettingsFactory = funcAssembly.GetType("Azure.Functions.Cli.Common.SelfHostWebHostSettingsFactory");
      var create = selfHostWebHostSettingsFactory.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
      var settings = create.Invoke(null, new object[] { traceLevel, scriptPath });

      // Setup(); // skip this, it just does some URLACL validation with an ugly popup
      var baseAddress = new Uri("http://localhost:7071/");

      // ReadSecrets(scriptPath, baseAddress); // skip this, it's hardcoded to use Environment.CurrentDirectory

      var selfHostAssembly = Assembly.LoadFrom(Path.Combine(azureFunctionsCoreTools, "System.Web.Http.SelfHost.dll"));

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
      var webHostAssembly = Assembly.LoadFile(Path.Combine(azureFunctionsCoreTools, "Microsoft.Azure.WebJobs.Script.WebHost.dll"));
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
