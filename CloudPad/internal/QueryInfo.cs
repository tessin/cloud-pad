using System;
using System.Collections.Generic;

namespace CloudPad.Internal {
  class QueryInfo {
    private readonly object _query;

    public QueryInfo(object query) {
      _query = query;
    }

    public class ConnectionInfo {
      private object _repo;

      public string Provider {
        get {
          var repo = _repo;
          var repoType = repo.GetType();
          var databaseInfo = repoType.GetProperty("DatabaseInfo").GetValue(repo);
          var databaseInfoType = databaseInfo.GetType();

          return (string)databaseInfoType.GetProperty("Provider").GetValue(databaseInfo);
        }
      }

      public ConnectionInfo(object repo) {
        this._repo = repo;
      }
    }

    public ConnectionInfo GetConnectionInfo() {
      var query = _query;
      var queryType = query.GetType();

      var getConnectionInfo = queryType.GetMethod("GetConnectionInfo");
      var repo = getConnectionInfo.Invoke(_query, null);
      if (repo == null) {
        return null; // ok, no connection info
      } else {
        return new ConnectionInfo(repo);
      }
    }

    // ================

    public IEnumerable<string> GetFileReferences() {
      var query = _query;
      var queryType = query.GetType();

      return (IEnumerable<string>)queryType.GetProperty("FileReferences").GetValue(query, null);
    }

    // ================

    public class NuGetReference {
      private object _nuget;

      public string PackageID {
        get {
          return (string)_nuget.GetType().GetProperty("PackageID").GetValue(_nuget, null);
        }
      }

      public NuGetReference(object nuget) {
        this._nuget = nuget;
      }

      public string[] GetAssemblyReferences() {
        return (string[])_nuget.GetType().GetMethod("GetAssemblyReferences").Invoke(_nuget, new object[] { true }); // honorExclusion: true
      }
    }

    public List<NuGetReference> GetNuGetReferences() {
      var query = _query;
      var queryType = query.GetType();

      var list = new List<NuGetReference>();

      foreach (var nuget in (System.Collections.IEnumerable)queryType.GetProperty("NuGetReferences").GetValue(query, null)) {
        list.Add(new NuGetReference(nuget));
      }

      return list;
    }
  }
}
