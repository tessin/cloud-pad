using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CloudPad.Internal {
  class CompilationOptions {
    public string QueryPath { get; }
    public string QueryDirectoryName { get; }
    public string QueryName { get; }

    public string OutDir { get; set; }

    public CompilationOptions(string queryPath) {
      if (queryPath == null) {
        throw new ArgumentNullException(nameof(queryPath));
      }
      this.QueryPath = queryPath;
      this.QueryDirectoryName = Path.GetDirectoryName(queryPath);
      this.QueryName = Path.GetFileNameWithoutExtension(queryPath);
    }
  }

  static class Compiler {
    public static void Compile(UserQueryTypeInfo userQuery, CompilationOptions options) {
      if (options.OutDir == null) {
        throw new InvalidOperationException("Compilation option 'OutDir' cannot be null");
      }

      // ====

      var functions = FunctionBinder.BindAll(userQuery.Type);

      // ====

      var assemblyName = typeof(Program).Assembly.GetName();
      var generatedBy = assemblyName.Name + "-" + assemblyName.Version.Major + "." + assemblyName.Version.Minor + "." + assemblyName.Version.Build; // .NET calls build what semver calls revision

      foreach (var f in functions) {
        var functionJson = new JObject();

        functionJson["generatedBy"] = assemblyName.Name + "-" + assemblyName.Version.Major + "." + assemblyName.Version.Minor + "." + assemblyName.Version.Build; // .NET calls build what semver calls revision

        // "attributes" is used by the Azure Web Jobs SDK 
        // to bind using metadata and takes precedence 
        // over "config". we do not want this.
        functionJson["configurationSource"] = "config";

        functionJson["bindings"] = JToken.FromObject(f.Trigger.GetBindings());
        functionJson["disabled"] = false;
        functionJson["scriptFile"] = "../bin/CloudPad.FunctionApp.dll";
        functionJson["entryPoint"] = f.Trigger.GetEntryPoint();

        var cloudPad = new JObject();

        cloudPad["applicationBase"] = $"../scripts/{options.QueryName}_{userQuery.Id}";
        cloudPad["scriptFile"] = userQuery.AssemblyLocationFileName;
        cloudPad["typeName"] = userQuery.Type.FullName;
        cloudPad["methodName"] = f.Method.Name;

        functionJson["cloudPad"] = cloudPad;

        // ====  

        var functionDir = Path.Combine(options.OutDir, options.QueryName + "_" + f.Method.Name);
        Directory.CreateDirectory(functionDir);
        File.WriteAllText(Path.Combine(functionDir, "function.json"), JsonConvert.SerializeObject(functionJson, Formatting.Indented));
      }

      // ====

      var userAssemblies = LoadAllUserAssemblies(AppDomain.CurrentDomain);

      var lib = Path.Combine(options.OutDir, "scripts", options.QueryName + "_" + userQuery.Id);

      Directory.CreateDirectory(lib);

      foreach (var location in userAssemblies) {
        var destination = Path.Combine(lib, Path.GetFileName(location));
        if (File.Exists(destination)) {
          continue;
        }
        File.Copy(location, destination);
      }

      // ====

      var root = VirtualFileSystemRoot.GetRoot();

      root.SaveTo(lib);
    }

    private static List<string> LoadAllUserAssemblies(AppDomain appDomain) {
      var linqPadAssembly = Assembly.Load("LINQPad");

      var visited = new HashSet<string> { linqPadAssembly.FullName };

      foreach (var r in linqPadAssembly.GetReferencedAssemblies()) {
        visited.Add(r.FullName);
      }

      void WalkReferencedAssemblies(Assembly assembly) {
        if (assembly.IsDynamic) {
          return;
        }
        foreach (var assemblyRef in assembly.GetReferencedAssemblies()) {
          if (visited.Add(assemblyRef.FullName)) {
            Assembly referencedAssembly;
            try {
              referencedAssembly = appDomain.Load(assemblyRef);
            } catch {
              // track
              Debug.WriteLine($"Cannot load assembly '{assemblyRef}' referenced by '{assembly.GetName()}'");
              throw;
            }
            WalkReferencedAssemblies(referencedAssembly);
          }
        }
      };

      foreach (var assembly in appDomain.GetAssemblies()) {
        if (visited.Add(assembly.FullName)) {
          WalkReferencedAssemblies(assembly);
        }
      }

      // There are alot of assemblies that don't need to be added since they are provded by the hosting environment
      // we filter out these here, it's important that we don't filter out actual dependencies because if we do
      // the code won't run

      var exclude = new[]{
          // Ignore stuff from the LINQPad installation dir
        CanonicalDirectoryName(Path.GetDirectoryName(linqPadAssembly.Location)),

          // Ignore stuff from the Windows dir
        CanonicalDirectoryName(Environment.GetEnvironmentVariable("WINDIR")),
      };

      var excludeAssemblyByFullName = new HashSet<string> {
        typeof(System.Web.Http.IHttpActionResult).Assembly.FullName // ASP.NET MVC 5 stack
      };

      // If it's a reference of CloudPad we don't need to pack it
      var cloudPadAssembly = typeof(Program).Assembly;
      excludeAssemblyByFullName.Add(cloudPadAssembly.FullName);
      foreach (var assemblyRef in cloudPadAssembly.GetReferencedAssemblies()) {
        excludeAssemblyByFullName.Add(assemblyRef.FullName);
      }

      var list = new List<string>();

      foreach (var assembly in appDomain.GetAssemblies().OrderBy(x => x.FullName)) {
        if (assembly.IsDynamic) {
          continue;
        }

        var assemblyFullPath = assembly.Location;

        // Root out assemblies that are not files on disk

        if (string.IsNullOrEmpty(assemblyFullPath)) {
          continue;
        }

        if (!File.Exists(assemblyFullPath)) {
          continue;
        }

        // ====

        // Ignore assemblies that originate in the .NET Framework (or LINQPad)

        var skip = false;
        foreach (var prefix in exclude) {
          if (assemblyFullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            skip = true;
            break;
          }
        }
        if (skip) {
          Debug.WriteLine($"Excluded '{assembly.FullName}'", nameof(Compiler));
          continue;
        }

        // ====

        if (excludeAssemblyByFullName.Contains(assembly.FullName)) {
          Debug.WriteLine($"Excluded '{assembly.FullName}'", nameof(Compiler));
          continue;
        }

        list.Add(assemblyFullPath);

        Debug.WriteLine($"Included '{assembly.FullName}'\n  from '{assemblyFullPath}'", nameof(Compiler));
      }

      return list;
    }

    private static string CanonicalDirectoryName(string path) {
      if (path.EndsWith("\\")) {
        return path;
      }
      return path + "\\";
    }
  }
}
