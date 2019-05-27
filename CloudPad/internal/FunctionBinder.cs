using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CloudPad.Internal {
  static class FunctionBinder {
    private static readonly Type[] _triggerAttributeTypes = new Type[] {
      typeof(HttpTriggerAttribute),
      typeof(QueueTriggerAttribute),
    };

    public static FunctionDescriptor Bind(MethodInfo m) {
      ITriggerAttribute trigger = null;

      foreach (var triggerAttributeType in _triggerAttributeTypes) {
        var t = (ITriggerAttribute)m.GetCustomAttribute(triggerAttributeType, false);
        if (t != null) {
          if (trigger != null) {
            throw new ApplicationException($"Function '{m.Name}' cannot have multiple triggers.");
          }
          trigger = t;
        }
      }

      if (trigger == null) {
        return null;
      }

      var parameters = m.GetParameters();
      var parameterBindings = new FunctionParameterBindings(parameters.Length);

      foreach (var p in parameters) {
        var pt = p.ParameterType;
        if (parameterBindings.HasBinding(pt)) {
          throw new ApplicationException($"Function '{m.Name}' parameter '{p.Name}' type '{pt.FullName}' has already been bound once (you should remove this parameter).");
        }
        parameterBindings.Bind(pt, p.Position);
      }

      var triggerRequiredParameterTypes = trigger.GetRequiredParameterTypes();

      foreach (var triggerRequiredParameterType in triggerRequiredParameterTypes) {
        if (!parameterBindings.HasBinding(triggerRequiredParameterType)) {
          throw new ApplicationException($"Function '{m.Name}' is missing required parameter of type '{triggerRequiredParameterType}' (you need to add a parameter of this type).");
        }
      }

      var constructors = m.DeclaringType.GetConstructors();
      if (!(constructors.Length == 1)) {
        throw new ApplicationException($"Declaring type '{m.DeclaringType}' of function '{m.Name}' should have exactly 1 constructor.");
      }

      IUserQueryActivator activator;

      var constructor = constructors[0];
      var constructorParameters = constructor.GetParameters();
      switch (constructorParameters.Length) {
        case 0: {
          activator = new UserQueryActivator(m.DeclaringType);
          break; // ok, parameterless
        }
        case 1: {
          var constructorParameter0 = constructorParameters[0];
          if (constructorParameter0.ParameterType == typeof(System.Data.IDbConnection)) {
            activator = new UserQueryWithConnectionActivator(m.DeclaringType);
            break; // ok, typed data context
          }
          goto default;
        }
        default:
          throw new ApplicationException($"Declaring type '{m.DeclaringType}' of function '{m.Name}' has an unsupported constructor signature.");
      }

      var f = new FunctionDescriptor(m, trigger, parameterBindings, activator);
      return f;
    }

    public static List<FunctionDescriptor> BindAll(Type userQuery) {
      var functions = new List<FunctionDescriptor>();

      var exclude = new HashSet<string>(typeof(object).GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(m => m.Name));

      foreach (var m in userQuery.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
        if (exclude.Contains(m.Name)) {
          continue; // if you for some reason decide to override .ToString or whatever...
        }
        var f = Bind(m);
        if (f != null) {
          // assert that this name is not ambigous
          try {
            userQuery.GetMethod(m.Name, BindingFlags.Public | BindingFlags.Instance);
          } catch (AmbiguousMatchException) {
            throw new ApplicationException($"You have more than 1 public instance method with the name '{m.Name}' in your script. This results in an ambigous match exception. Make the non-function methods, private (or static).");
          }
          functions.Add(f);
        } else {
          Trace.WriteLine($"Public non-static method '{m.Name}' (ignored) does not have a function trigger attribute. To squash this warning make the method private.", nameof(FunctionBinder));
        }
      }

      Trace.WriteLine($"Found {functions.Count} function(s).", nameof(FunctionBinder));

      return functions;
    }
  }
}