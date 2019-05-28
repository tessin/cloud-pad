using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CloudPad.Internal
{
    class CompilationOptions
    {
        public string QueryPath { get; }
        public string QueryDirectoryName { get; }
        public string QueryName { get; }

        public string OutDir { get; set; }
        public bool Delta { get; set; }

        public CompilationOptions(string queryPath)
        {
            if (queryPath == null)
            {
                throw new ArgumentNullException(nameof(queryPath));
            }
            this.QueryPath = queryPath;
            this.QueryDirectoryName = Path.GetDirectoryName(queryPath);
            this.QueryName = Path.GetFileNameWithoutExtension(queryPath);
        }
    }

    static class Compiler
    {
        public static void Compile(UserQueryTypeInfo userQuery, QueryInfo currentQuery, CompilationOptions options, QueryInfo currentQueryInfo)
        {
            Debug.WriteLine("==== Compiler pass begin ====", nameof(Compiler));

            // ====

            if (options.OutDir == null)
            {
                throw new InvalidOperationException("Compilation option 'OutDir' cannot be null");
            }

            // ====

            var functions = FunctionBinder.BindAll(userQuery.Type);

            // ====

            var functionDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ====

            var assemblyName = typeof(Program).Assembly.GetName();
            var generatedBy = assemblyName.Name + "-" + assemblyName.Version.Major + "." + assemblyName.Version.Minor + "." + assemblyName.Version.Build; // .NET calls build what semver calls revision

            foreach (var f in functions)
            {
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
                if (connInfo != null)
                {
                    cloudPad["providerName"] = connInfo.Provider;
                    cloudPad["connectionString"] = Util.CurrentCxString;
                }

                functionJson["cloudPad"] = cloudPad;

                // ====  

                var functionName = options.QueryName + "_" + f.Method.Name;
                var functionDir = Path.Combine(options.OutDir, functionName);
                Directory.CreateDirectory(functionDir);
                File.WriteAllText(Path.Combine(functionDir, "function.json"), JsonConvert.SerializeObject(functionJson, Formatting.Indented));
                functionDirs.Add(functionName);
            }

            if (Directory.Exists(options.OutDir))
            {
                foreach (var d in Directory.EnumerateDirectories(options.OutDir, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(d);
                    if ("bin".Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if ("scripts".Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (!functionDirs.Contains(name))
                    {
                        Directory.Delete(d, true);
                    }
                }
            }

            // ====

            var userAssemblies = LoadAllUserAssemblies2(userQuery, currentQuery);

            // ====

            var lib = Path.Combine(options.OutDir, "scripts", options.QueryName + "_" + userQuery.Id);
            Directory.CreateDirectory(lib);
            foreach (var userAssembly in userAssemblies)
            {
                var destination = Path.Combine(lib, Path.GetFileName(userAssembly.Name.Name + ".dll")); // stabilize DLL name (simplifies assembly resolve)
                if (File.Exists(destination))
                {
                    Debug.WriteLine($"Output file exists '{destination}'", nameof(Compiler));
                    continue;
                }
                AssemblyBindingRewrite.Rewrite(userAssembly.Location, destination);
            }

            // ====

            var root = VirtualFileSystemRoot.GetRoot();
            root.SaveTo(lib);

            // ====

            Debug.WriteLine($"==== Compiler pass end, OutDir= {options.OutDir} ====", nameof(Compiler));
        }

        private static List<AssemblyCandidate> LoadAllUserAssemblies2(UserQueryTypeInfo userQuery, QueryInfo currentQuery)
        {
            // strategy for finding what assembly version to bundle
            // whenever there is a version ambiguity, we will remove the version that CloudPad referenced
            // (multiple versions show up because users bring in different code not same)

            var currentDomain = AppDomain.CurrentDomain;

            var cs = new AssemblyCandidateSet();

            // the purpose of this code is to get the typed data context, if used

            cs.Add(userQuery.Assembly.Location, userQuery.Assembly.GetName(), "<UserQuery>");

            foreach (var r in userQuery.Assembly.GetReferencedAssemblies())
            {
                var b = currentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == r.FullName); // if exact
                if (b != null)
                {
                    cs.Add(b.Location, r, "<UserQuery>");
                }
                else
                {
                    var referencedAssembly = Assembly.Load(r.FullName);
                    if (referencedAssembly.FullName == r.FullName)
                    {
                        cs.Add(referencedAssembly.Location, r, "<UserQuery>");
                    }
                }
            }

            // the rest is solved for us by LINQPad

            foreach (var f in currentQuery.GetFileReferences())
            {
                var name = AssemblyName.GetAssemblyName(f);
                cs.Add(f, name, "<File>");
            }

            foreach (var nuget in currentQuery.GetNuGetReferences())
            {
                var packageID = nuget.PackageID;
                foreach (var f in nuget.GetAssemblyReferences())
                {
                    var name = AssemblyName.GetAssemblyName(f);
                    cs.Add(f, name, packageID);
                }
            }

            // ====

            void unrefReferencedAssemblies(Assembly assembly)
            {
                foreach (var r in assembly.GetReferencedAssemblies())
                {
                    Debug.WriteLine($"Unref '{r.FullName}'", nameof(Compiler));
                    if (cs.Unref(r))
                    {
                        var b = currentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == r.FullName); // if exact
                        if (b != null)
                        {
                            unrefReferencedAssemblies(b);
                        }
                        else
                        {
                            Debug.WriteLine($"UnrefLoad '{r}'", nameof(Compiler));
                            var referencedAssembly = Assembly.Load(r.FullName);
                            Debug.WriteLine($"UnrefLoaded '{referencedAssembly.FullName}'\n from location '{referencedAssembly.Location}'", nameof(Compiler));
                            if (referencedAssembly.FullName == r.FullName)
                            { // if exact
                                unrefReferencedAssemblies(referencedAssembly);
                            }
                        }
                    }
                }
            }

            var cp = typeof(Program).Assembly; // CloudPad assembly

            // Unref everything that CloudPad brings in
            // we don't need it and it will intermingle 
            // with other versions which we don't want.

            if (cs.Unref(cp.GetName()))
            {
                unrefReferencedAssemblies(cp);
            }

            // ====

            var fs = new List<AssemblyCandidate>();

            // ====

            var excludeLocations = new List<string>() {
                // Ignore stuff from LINQPad installation dir
                CanonicalDirectoryName(Path.GetDirectoryName(Assembly.Load("LINQPad").Location)),

                // Ignore stuff from azure-functions-core-tools installation dir
                CanonicalDirectoryName(Env.GetProgramDataDirectory()), // only necessary when running from within LINQPad but it doesn't hurt

                // Ignore stuff from Windows installation dir
                CanonicalDirectoryName(Environment.GetEnvironmentVariable("WINDIR")),
            };

            bool shouldExcludeLocation(string location)
            {
                foreach (var excludeLocation in excludeLocations)
                {
                    if (location.StartsWith(excludeLocation, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }

            // ====

            foreach (var item in cs.Set)
            {
                var list = item.Value;
                var c = list[list.Count - 1]; // use highest version
                if (1 < list.Count)
                {
                    Trace.WriteLine($"Warning: Multiple versions of assembly '{c.Name.Name}' found.");
                    Trace.WriteLine($"Warning: Assembly with highest version '{c.Name.FullName}' from location '{c.Location}' used.");
                    Trace.WriteLine("Warning: The following verion(s) will not be used:");
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        var d = list[i];
                        Trace.WriteLine($"Warning:   Assembly '{d.FullName}' from location '{d.Location}' ignored.");
                    }
                }
                if (shouldExcludeLocation(c.Location))
                {
                    continue;
                }
                fs.Add(c);
            };

            // ====

            return fs;
        }

        private static string CanonicalDirectoryName(string path)
        {
            if (path.EndsWith("\\"))
            {
                return path;
            }
            return path + "\\";
        }
    }
}
