using System;

namespace CloudPad.Internal {
  interface ITriggerAttribute {
    Type[] GetRequiredParameterTypes();
    object GetBindings();
    string GetEntryPoint();
  }
}