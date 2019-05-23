using CloudPad.Internal;
using System;

namespace CloudPad {

  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public class TimerTriggerAttribute : Attribute, ITriggerAttribute {
    public TimerTriggerAttribute(string blobPath) {
    }

    object ITriggerAttribute.GetBindings() {
      throw new NotImplementedException();
    }

    string ITriggerAttribute.GetEntryPoint() {
      throw new NotImplementedException();
    }

    Type[] ITriggerAttribute.GetRequiredParameterTypes() {
      throw new NotImplementedException();
    }
  }
}
