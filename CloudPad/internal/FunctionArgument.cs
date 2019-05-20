using System;
using System.Collections.Generic;

namespace CloudPad.Internal {
  struct FunctionArgument {
    public readonly Type ParameterType;
    public readonly object Value;

    public FunctionArgument(Type parameterType, object value) {
      this.ParameterType = parameterType;
      this.Value = value;
    }
  }

  class FunctionArgumentList {
    private readonly List<FunctionArgument> _list = new List<FunctionArgument>();

    public void AddArgument(Type parameterType, object value) {
#if DEBUG
      if (parameterType == null) {
        throw new ArgumentNullException(nameof(parameterType));
      }
      if (value == null) {
        throw new ArgumentNullException(nameof(value));
      }
      if (!parameterType.IsAssignableFrom(value.GetType())) {
        throw new ArgumentException(); // pedantic
      }
      foreach (var arg in _list) {
        if (arg.ParameterType == parameterType) {
          throw new ArgumentException(); // pedantic
        }
      }
#endif
      _list.Add(new FunctionArgument(parameterType, value));
    }

    public object FindValue(Type parameterType) {
      foreach (var arg in _list) {
        if (arg.ParameterType == parameterType) {
          return arg.Value;
        }
      }
      return null;
    }

    public object[] Apply(FunctionParameterBindings parameterBindings) {
      var values = new object[parameterBindings.Arity];
      for (int i = 0; i < values.Length; i++) {
        var binding = parameterBindings.GetBinding(i);
        var value = FindValue(binding.ParameterType);
        if (value == null) {
          values[i] = binding.DefaultValue;
        } else {
          values[i] = value;
        }
      }
      return values;
    }
  }
}