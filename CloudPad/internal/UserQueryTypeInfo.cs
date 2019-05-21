using System;
using System.IO;
using System.Reflection;

namespace CloudPad.Internal {
  class UserQueryTypeInfo {
    public Type Type { get; }
    public Assembly Assembly { get; }
    public string AssemblyLocation { get; }
    public string AssemblyLocationFileName { get; }
    public string AssemblyName { get; }
    public string Id { get; }

    public UserQueryTypeInfo(object userQuery) {
      Type = userQuery.GetType();
      Assembly = Type.Assembly;
      AssemblyLocation = Assembly.Location;
      AssemblyLocationFileName = Path.GetFileName(AssemblyLocation);
      AssemblyName = Assembly.GetName().Name;

      var id = AssemblyName; // the unqiue ID for the compilation shaved of the simple assembly name
      if (id.StartsWith("query_")) {
        id = id.Substring("query_".Length);
      }
      Id = id;
    }
  }
}
