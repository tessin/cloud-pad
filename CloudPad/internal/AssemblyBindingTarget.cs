using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CloudPad.Internal
{
    static class AssemblyBindingTarget
    {
        // this is based on the assembly binding redirects of the func.exe.config file

        // from 0.0.0.0- up to the specific version
        private static readonly Dictionary<string, Version> bindingRedirects = new Dictionary<string, Version> {
            { "Newtonsoft.Json", new Version(9, 0, 0, 0) },
        };

        public static void Rewrite(string source, string destination)
        {
            var assembly = AssemblyDefinition.ReadAssembly(source);

            bool rewrite = false;

            foreach (var r in assembly.MainModule.AssemblyReferences)
            {
                if (bindingRedirects.TryGetValue(r.Name, out var maxVersion))
                {
                    if (maxVersion < r.Version)
                    {
                        Debug.WriteLine($"Assembly '{source}' has reference to '{r}' which was redirected to version '{maxVersion}'");
                        r.Version = maxVersion;
                        rewrite = true;
                    }
                }
            }

            if (rewrite)
            {
                assembly.Write(destination);
            }
            else
            {
                File.Copy(source, destination);
            }
        }
    }
}
