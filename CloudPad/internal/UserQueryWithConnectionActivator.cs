using System;
using System.Data;
using System.Data.Common;

namespace CloudPad.Internal {
  class UserQueryWithConnectionActivator : IUserQueryActivator {
    class Scope : IUserQueryActivatorScope {
      private Type _userQuery;
      private object _conn;

      public Scope(Type userQuery, IDbConnection conn) {
        _userQuery = userQuery;
        _conn = conn;
      }

      public object CreateInstance() {
        return Activator.CreateInstance(_userQuery, new object[] { _conn });
      }

      public void Dispose() {
        var dispoable = _conn as IDisposable;
        if (dispoable != null) {
          dispoable.Dispose();
        }
      }
    }

    private Type _userQuery;

    public UserQueryWithConnectionActivator(Type userQuery) {
      this._userQuery = userQuery;
    }

    public IUserQueryActivatorScope CreateScope(IFunctionMetadata f) {
      var conn = DbProviderFactories.GetFactory(f.ProviderName).CreateConnection();
      conn.ConnectionString = f.ConnectionString;
      conn.Open();
      return new Scope(_userQuery, conn);
    }
  }
}
