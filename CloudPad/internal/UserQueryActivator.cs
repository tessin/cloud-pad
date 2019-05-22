using System;

namespace CloudPad.Internal {
  class UserQueryActivator : IUserQueryActivator {
    class Scope : IUserQueryActivatorScope {
      private Type _userQuery;

      public Scope(Type userQuery) {
        this._userQuery = userQuery;
      }

      public object CreateInstance() {
        return Activator.CreateInstance(_userQuery);
      }

      public void Dispose() {
        // no op
      }
    }

    private readonly Type _userQuery;

    public UserQueryActivator(Type userQuery) {
      this._userQuery = userQuery;
    }

    public IUserQueryActivatorScope CreateScope(IFunctionMetadata f) {
      return new Scope(_userQuery);
    }
  }
}
