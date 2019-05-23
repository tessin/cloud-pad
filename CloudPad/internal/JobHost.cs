using System;
using System.Collections.Generic;
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
        var req = WebRequest.Create($"https://functionscdn.azureedge.net/public/{version}/Azure.Functions.Cli.zip");
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

    public static void Prepare() {
      // this should be done exactly once before any call to `LaunchAsync`

      var azureFunctionsCoreTools = GetAzureFunctionsCoreTools("1.0.19");

      var funcConfig = System.Xml.Linq.XElement.Load(Path.Combine(azureFunctionsCoreTools, "func.exe.config"));
      var runtime = funcConfig.Element("runtime");

      var assemblyBindingRedirects = new Dictionary<string, object>();

      System.Xml.Linq.XNamespace ns = "urn:schemas-microsoft-com:asm.v1";
      var assemblyBinding = runtime.Element(ns + "assemblyBinding");
      var assemblyIdentityName = ns + "assemblyIdentity";
      var bindingRedirectName = ns + "bindingRedirect";
      foreach (var dependentAssembly in assemblyBinding.Elements(ns + "dependentAssembly")) {
        var assemblyIdentity = dependentAssembly.Element(assemblyIdentityName);

        var fullName = (string)assemblyIdentity.Attribute("name");

        if ((string)assemblyIdentity.Attribute("culture") != null) {
          fullName += ", Culture=" + (string)assemblyIdentity.Attribute("culture");
        }

        if ((string)assemblyIdentity.Attribute("publicKeyToken") != null) {
          fullName += ", PublicKeyToken=" + new StrongNameKeyPair((string)assemblyIdentity.Attribute("publicKeyToken"));
        }

        var assemblyName = new AssemblyName(fullName);

        var bindingRedirect = dependentAssembly.Element(bindingRedirectName);
        var oldVersion = ((string)bindingRedirect.Attribute("oldVersion")).Split('-');
        assemblyBindingRedirects[assemblyName.FullName] = new {
          minVersion = new Version(oldVersion[0]),
          maxVersion = new Version(oldVersion[1]),
          newVersion = new Version((string)bindingRedirect.Attribute("newVersion")),
        };
      }

      Extensions.Dump(assemblyBindingRedirects);

      AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
        // note: e.RequestingAssembly is always null (for some reason?)

        Debug.WriteLine($"AssemblyResolve '{e.Name}'", "func.exe");

        var name = new AssemblyName(e.Name);

        var probePaths = new[] {
          Path.Combine(azureFunctionsCoreTools, name.Name + ".dll"), // DLL first
          Path.Combine(azureFunctionsCoreTools, name.Name + ".exe"),
        };

        foreach (var probePath in probePaths) {
          if (File.Exists(probePath)) {
            var probeAssemblyName = AssemblyName.GetAssemblyName(probePath);
            if (probeAssemblyName.FullName == e.Name) {
              Debug.WriteLine($"ResolvedAssembly '{e.Name}'", "func.exe");

              return Assembly.LoadFrom(probePath);
            }
          }
        }

        Debug.WriteLine($"UnresolvedAssembly '{e.Name}'", "func.exe");
        return null;
      };

      //var funcAssembly = Assembly.LoadFrom(Path.Combine(azureFunctionsCoreTools, "func.exe"));

      //var pass2 = new List<Assembly>();

      //foreach (var r in funcAssembly.GetReferencedAssemblies()) {
      //  var probePaths = new[] {
      //    Path.Combine(azureFunctionsCoreTools, r.Name + ".dll"), // DLL first
      //    Path.Combine(azureFunctionsCoreTools, r.Name + ".exe"),
      //  };

      //  foreach (var probePath in probePaths) {
      //    if (File.Exists(probePath)) {
      //      Debug.WriteLine($"ResolvedAssembly '{r.Name}' form location '{probePath}'", "func.exe");
      //      pass2.Add(Assembly.LoadFrom(probePath));
      //    }
      //  }
      //}

      //foreach (var funcAssembly2 in pass2) {
      //  foreach (var r in funcAssembly2.GetReferencedAssemblies()) {
      //    var probePaths = new[] {
      //    Path.Combine(azureFunctionsCoreTools, r.Name + ".dll"), // DLL first
      //    Path.Combine(azureFunctionsCoreTools, r.Name + ".exe"),
      //  };

      //    foreach (var probePath in probePaths) {
      //      if (File.Exists(probePath)) {
      //        Debug.WriteLine($"ResolvedAssembly '{r.Name}' form location '{probePath}'", "func.exe");
      //        Assembly.LoadFrom(probePath);
      //      }
      //    }
      //  }
      //}
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

      // this implementation is worth revising with respect to the following documentation
      // https://docs.microsoft.com/en-us/dotnet/framework/app-domains/resolve-assembly-loads?view=netframework-4.8

      // for some reason, I ran into a failure to resolve "System.Runtime.Loader"
      // the next day after I restarted all applications (not a reboot) the problem wasn't there
      // and I used procmon to note that "System.Runtime.Loader" was loaded from GAC

      //AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
      //  Debug.WriteLine($"AssemblyResolve '{e.Name}'", "func.exe");
      //  if (e.RequestingAssembly != null) {
      //    Debug.WriteLine($"  RequestingAssembly '{e.RequestingAssembly}'", "func.exe");
      //  }

      //  //I don't remember precisly if this is useful or not, couldn't tell that it was, so...
      //  //foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
      //  //  if (assembly.FullName == e.Name) {
      //  //    Debug.WriteLine($"ResolvedAssembly '{e.Name}' from loaded set of assemblies", "func.exe");

      //  //    return assembly;
      //  //  }
      //  //}

      //  var name = new AssemblyName(e.Name);

      //  var probePaths = new[] {
      //    Path.Combine(azureFunctionsCoreTools, name.Name + ".dll"), // DLL first
      //    Path.Combine(azureFunctionsCoreTools, name.Name + ".exe"),
      //  };

      //  foreach (var probePath in probePaths) {
      //    if (File.Exists(probePath)) {
      //      Debug.WriteLine($"ResolvedAssembly '{e.Name}' form location '{probePath}'", "func.exe");
      //      return Assembly.LoadFrom(probePath);
      //    }
      //  }

      //  Debug.WriteLine($"UnresolvedAssembly '{e.Name}'", "func.exe");
      //  return null;
      //};

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
