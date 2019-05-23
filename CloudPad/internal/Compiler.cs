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
    public static void Compile(UserQueryTypeInfo userQuery, QueryInfo currentQuery, CompilationOptions options, QueryInfo currentQueryInfo) {
      Debug.WriteLine("==== Compiler pass begin ====", nameof(Compiler));

      // ====

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

        var connInfo = currentQueryInfo.GetConnectionInfo();
        if (connInfo != null) {
          cloudPad["providerName"] = connInfo.Provider;
          cloudPad["connectionString"] = Util.CurrentCxString;
        }

        functionJson["cloudPad"] = cloudPad;

        // ====  

        var functionDir = Path.Combine(options.OutDir, options.QueryName + "_" + f.Method.Name);
        Directory.CreateDirectory(functionDir);
        File.WriteAllText(Path.Combine(functionDir, "function.json"), JsonConvert.SerializeObject(functionJson, Formatting.Indented));
      }

      // ====

      var userAssemblies = LoadAllUserAssemblies2(userQuery, currentQuery);

      // ====

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

      // ====

      Debug.WriteLine($"==== Compiler pass end (out='{options.OutDir}') ====", nameof(Compiler));
    }

    class CandidateSet {
      public class Candidate {
        public string FullName => Name.FullName;
        public AssemblyName Name { get; set; }
        public Version Version => Name.Version;
        public string Location { get; set; }
        public string Source { get; set; }
      }

      public class CandidateList {
        public List<Candidate> Candidates { get; } = new List<Candidate>();

        public void Add(Candidate candidate) {
          if (Candidates.Any(c => c.Version == candidate.Version)) {
            return; // has version
          }
          Candidates.Add(candidate);
        }
      }

      private readonly Dictionary<string, CandidateList> _d = new Dictionary<string, CandidateList>();

      public IEnumerable<KeyValuePair<string, CandidateList>> Set {
        get { return _d.OrderBy(x => x.Key); }
      }

      public void Add(string f, string source) {
        var name = AssemblyName.GetAssemblyName(f);
        if (!_d.TryGetValue(name.Name, out var list)) {
          _d.Add(name.Name, list = new CandidateList());
        }
        list.Add(new Candidate { Name = name, Location = f, Source = source });
      }
    }

    private static List<string> LoadAllUserAssemblies2(UserQueryTypeInfo userQuery, QueryInfo currentQuery) {

      // strategy for finding what assembly version to bundle

      // whenever there is a version ambiguity, we will remove the version that CloudPad referenced
      // (multiple versions show up because users bring in different code not same)


      var cs = new CandidateSet();

      foreach (var f in currentQuery.GetFileReferences()) {
        cs.Add(f, f);
      }

      foreach (var nuget in currentQuery.GetNuGetReferences()) {
        var packageID = nuget.PackageID;
        foreach (var f in nuget.GetAssemblyReferences()) {
          cs.Add(f, packageID);
        }
      }

      Extensions.Dump(cs);

      // ====

      var list = new List<Assembly> { userQuery.Assembly };

      // ====

      var cp = typeof(Program).Assembly; // CloudPad assembly

      // ====

      var excludeFullName = new HashSet<string> {
        cp.FullName,
      };
      foreach (var r in cp.GetReferencedAssemblies()) {
        excludeFullName.Add(r.FullName);
      }

      // ====

      var excludeLocations = new List<string>() {
        // Ignore stuff from LINQPad installation dir
        CanonicalDirectoryName(Path.GetDirectoryName(Assembly.Load("LINQPad").Location)),

        // Ignore stuff from azure-functions-core-tools installation dir
        CanonicalDirectoryName(Env.GetProgramDataDirectory()), // only necessary when running from within LINQPad but it doesn't hurt

        // Ignore stuff from Windows installation dir
        CanonicalDirectoryName(Environment.GetEnvironmentVariable("WINDIR")),
      };

      bool shouldExcludeLocation(string location) {
        foreach (var excludeLocation in excludeLocations) {
          if (location.StartsWith(excludeLocation, StringComparison.OrdinalIgnoreCase)) {
            return true;
          }
        }
        return false;
      }

      // ====

      void walkReferencedAssemblies(Assembly assembly) {
        foreach (var r in assembly.GetReferencedAssemblies()) {
          if (excludeFullName.Contains(r.FullName)) {
            continue;
          }
          Debug.WriteLine($"ReferencedAssembly '{r}'", nameof(Compiler));
          var referencedAssembly = Assembly.Load(r.FullName);
          Debug.WriteLine($"Assembly loaded from '{referencedAssembly.Location}'", nameof(Compiler));
          if (shouldExcludeLocation(referencedAssembly.Location)) {
            Debug.WriteLine($"Assembly excluded by location", nameof(Compiler));
            continue;
          }
          list.Add(referencedAssembly);
          walkReferencedAssemblies(referencedAssembly);
        }
      }

      walkReferencedAssemblies(userQuery.Assembly);

      // ====

      foreach (var g in list.Select(x => {
        var name = x.GetName();
        return new {
          name.Name,
          name.Version,
          Assembly = x
        };
      }).GroupBy(x => x.Name).OrderBy(x => x.Key)) {
        if (1 < g.Count()) {
          Debug.WriteLine($"Assembly '{g.Key}' has multiple copies", nameof(Compiler));
          foreach (var assembly in g) {
            Debug.WriteLine($"  - '{assembly.Version}', '{assembly.Assembly.Location}'", nameof(Compiler));
          }
        }
      }

      // ====

      return list.Select(x => x.Location).OrderBy(x => x).ToList();
    }


    private static List<string> LoadAllUserAssemblies(List<FunctionDescriptor> functions) {

      //// This trick will ensure that the dependant assemblies get loaded
      //foreach (var f in functions) {
      //  System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(f.Method.MethodHandle);
      //}

      var linqPadAssembly = Assembly.Load("LINQPad");

      var visited = new HashSet<string> {
        linqPadAssembly.FullName,

        // Not supported on the desktop (but I don't understand why it's getting pulled in here)
        "System.Runtime.Loader, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
      };

      foreach (var r in linqPadAssembly.GetReferencedAssemblies().OrderBy(r => r.FullName)) {
        Debug.WriteLine($"Ignore assembly '{r.FullName}' referenced by LINQPad");
        visited.Add(r.FullName);
      }

      void WalkReferencedAssemblies(Assembly assembly) {
        if (assembly.IsDynamic) {
          return;
        }
        Debug.WriteLine($"Walk assembly '{assembly.FullName}'");
        foreach (var r in assembly.GetReferencedAssemblies().OrderBy(r => r.FullName)) {
          if (visited.Add(r.FullName)) {
            Debug.WriteLine($"Load assembly '{r.FullName}'");
            Assembly referencedAssembly;
            try {
              referencedAssembly = Assembly.Load(r);
              Debug.WriteLine($"Assembly '{r.FullName}' loaded from location '{referencedAssembly.Location}'");
            } catch {
              // track
              Debug.WriteLine($"Cannot load assembly '{r}' referenced by '{assembly.GetName()}'");
              throw;
            }
            WalkReferencedAssemblies(referencedAssembly);
          }
        }
      };

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
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

          // Ignore stuff from the azure-functions-core-tools installation dir
        CanonicalDirectoryName(Env.GetProgramDataDirectory()), // only necessary when running from within LINQPad but it doesn't hurt

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

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName)) {
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
