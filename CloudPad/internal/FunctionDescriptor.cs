using System.Reflection;

namespace CloudPad.Internal {
  class FunctionDescriptor {
    public MethodInfo Method { get; }
    public ITriggerAttribute Trigger { get; }
    public FunctionParameterBindings ParameterBindings { get; }
    public IUserQueryActivator Activator { get; }

    public FunctionDescriptor(MethodInfo method, ITriggerAttribute trigger, FunctionParameterBindings parameterBindings, IUserQueryActivator activator) {
      this.Method = method;
      this.Trigger = trigger;
      this.ParameterBindings = parameterBindings;
      this.Activator = activator;
    }
  }
}