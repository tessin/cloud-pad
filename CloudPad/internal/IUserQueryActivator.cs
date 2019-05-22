using System;

namespace CloudPad.Internal {
  interface IUserQueryActivator {
    IUserQueryActivatorScope CreateScope(IFunctionMetadata f);
  }

  interface IUserQueryActivatorScope : IDisposable {
    object CreateInstance();
  }
}
