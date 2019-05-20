using System;
using System.Collections.Generic;
using System.Reflection;

namespace CloudPad.Internal {
  struct FunctionParameterBinding {
    public readonly Type ParameterType;
    public readonly object DefaultValue;

    public bool IsEmpty => ParameterType == null;
    public bool HasValue => ParameterType != null;

    public FunctionParameterBinding(Type parameterType) {
      if (parameterType == null) {
        throw new ArgumentNullException(nameof(parameterType));
      }
      ParameterType = parameterType;
      DefaultValue = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
    }
  }

  class FunctionParameterBindings {
    private FunctionParameterBinding[] _bindings { get; }

    public int Arity => _bindings.Length;

    public FunctionParameterBindings(int arity) {
      _bindings = new FunctionParameterBinding[arity];
    }

    public FunctionParameterBinding FindBinding(Type parameterType) {
      if (parameterType == null) {
        throw new ArgumentNullException(nameof(parameterType));
      }
      var bindings = _bindings;
      for (int i = 0, c = bindings.Length; i < c; i++) {
        var binding = bindings[i];
        if (binding.ParameterType == parameterType) {
          return binding;
        }
      }
      return new FunctionParameterBinding();
    }

    public bool HasBinding(Type parameterType) {
      return FindBinding(parameterType).HasValue;
    }

    public void Bind(Type parameterType, int position) {
      if (HasBinding(parameterType)) {
        throw new ArgumentException(); // pedantic 
      }
      _bindings[position] = new FunctionParameterBinding(parameterType);
    }

    public FunctionParameterBinding GetBinding(int position) {
      var binding = _bindings[position];
      if (binding.IsEmpty) {
        throw new InvalidOperationException($"Position {position} is not bound to a parameter type");
      }
      return binding;
    }
  }
}