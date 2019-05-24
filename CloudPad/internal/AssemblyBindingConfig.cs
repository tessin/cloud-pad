using System;
using System.Collections.Generic;
using System.Reflection;

namespace CloudPad.Internal {
  class AssemblyBindingRedirect {
    public Version OldMinVersion { get; set; }
    public Version OldMaxVersion { get; set; }

    public Version NewVersion { get; set; }
  }

  class AssemblyBindingConfig {
    public static AssemblyBindingConfig LoadFrom(string path) {
      var config = new AssemblyBindingConfig();

      var funcConfig = System.Xml.Linq.XElement.Load(path);
      var runtime = funcConfig.Element("runtime");

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
          fullName += ", PublicKeyToken=" + (string)assemblyIdentity.Attribute("publicKeyToken");
        }

        var assemblyName = new AssemblyName(fullName);

        var bindingRedirect = dependentAssembly.Element(bindingRedirectName);

        var oldVersion = ((string)bindingRedirect.Attribute("oldVersion")).Split('-');
        var newVerison = (string)bindingRedirect.Attribute("newVersion");

        var binding = new AssemblyBindingRedirect {
          OldMinVersion = new Version(oldVersion[0]),
          OldMaxVersion = new Version(oldVersion[1]),
          NewVersion = new Version(newVerison),
        };

        config.Add(assemblyName.Name, binding);
      }

      return config;
    }

    private Dictionary<string, List<AssemblyBindingRedirect>> config = new Dictionary<string, List<AssemblyBindingRedirect>>();

    public void Add(string name, AssemblyBindingRedirect binding) {
      if (!config.TryGetValue(name, out var bindings)) {
        config.Add(name, bindings = new List<AssemblyBindingRedirect>());
      }
      bindings.Add(binding);
    }

    public AssemblyBindingRedirect Find(AssemblyName name) {
      if (config.TryGetValue(name.Name, out var bindings)) {
        return bindings.Find(binding => binding.OldMinVersion <= name.Version && name.Version <= binding.OldMaxVersion);
      }
      return null;
    }
  }
}
