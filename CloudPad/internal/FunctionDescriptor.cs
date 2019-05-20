using System;
using System.Collections.Generic;
using System.Reflection;

namespace CloudPad.Internal {
  class FunctionDescriptor {
    public MethodInfo Method { get; }
    public ITriggerAttribute Trigger { get; }
    public FunctionParameterBindings ParameterBindings { get; }

    public FunctionDescriptor(MethodInfo method, ITriggerAttribute trigger, FunctionParameterBindings parameterBindings) {
      this.Method = method;
      this.Trigger = trigger;
      this.ParameterBindings = parameterBindings;
    }
  }
}