using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public class Function
    {
        private readonly object _context;
        private readonly MethodInfo _method;
        private readonly ParameterInfo[] _parameters;

        public string Name => _method.Name;

        public Binding Binding { get; }

        public Function(object context, MethodInfo method, Binding binding)
        {
            _context = context;
            _method = method;
            _parameters = method.GetParameters();
            Binding = binding;
        }

        public void CheckSignature()
        {
            switch (Binding.Type)
            {
                case BindingType.HttpTrigger:
                    {
                        var returnTypes = new[] {
                            typeof(Task<HttpResponseMessage>),
                            typeof(HttpResponseMessage)
                        };

                        var parameterTypes = new[] {
                            typeof(HttpRequestMessage),
                            typeof(ITraceWriter),
                            typeof(CancellationToken),
                        };

                        CheckSignature(returnTypes, parameterTypes);
                        break;
                    }

                case BindingType.TimerTrigger:
                    {
                        var returnTypes = new[] {
                            typeof(Task),
                            typeof(void)
                        };

                        var parameterTypes = new[] {
                            typeof(ITraceWriter),
                            typeof(CancellationToken),
                        };

                        CheckSignature(returnTypes, parameterTypes);
                        break;
                    }

                default:
                    throw new NotImplementedException("unsupported binding");
            }
        }

        private void CheckSignature(Type[] returnTypes, Type[] parameterTypes)
        {
            if (!returnTypes.Any(t => t.IsAssignableFrom(_method.ReturnType)))
            {
                Log.Trace.Append($"method '{_method.Name}' return type must be one of {string.Join(", ", returnTypes.Select(t => t.FullName))}");
            }

            foreach (var parameter in _method.GetParameters())
            {
                if (!parameterTypes.Any(t => t.IsAssignableFrom(parameter.ParameterType)))
                {
                    Log.Trace.Append($"method '{_method.Name}' parameter '{parameter.Name}' type must be one of {string.Join(", ", parameterTypes.Select(t => t.FullName))}");
                }
            }
        }

        public Task InvokeAsync(
            HttpRequestMessage req = null,
            ITraceWriter log = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var parameterValues = new object[_parameters.Length];

            for (int i = 0; i < parameterValues.Length; i++)
            {
                var parameterType = _parameters[i].ParameterType;
                if (parameterType == typeof(HttpRequestMessage))
                {
                    parameterValues[i] = req;
                    continue;
                }
                if (parameterType == typeof(ITraceWriter))
                {
                    parameterValues[i] = log;
                    continue;
                }
                if (parameterType == typeof(CancellationToken))
                {
                    parameterValues[i] = cancellationToken;
                    continue;
                }
                throw new InvalidOperationException($"cannot bind parameter type {parameterType}");
            }

            var result = _method.Invoke(_context, parameterValues);

            if (result is Task resultTask)
            {
                return resultTask;
            }

            if (result is HttpResponseMessage res)
            {
                return Task.FromResult(res);
            }

            return Task.FromResult(result); // void
        }
    }

    public class FunctionIndex
    {
        private readonly object _context;

        public List<Function> Functions { get; } = new List<Function>();

        public FunctionIndex(object context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        public void Initialize(string name = null)
        {
            if (0 < Functions.Count) return;

            if (name == null)
            {
                var methods = _context.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                foreach (var g in methods.GroupBy(x => x.Name))
                {
                    if (1 < g.Count())
                    {
                        throw new InvalidOperationException($"overloading is not supported. (you cannot have two methods with the same name '{name}'). see {string.Join(", ", g)}");
                    }

                    var method = g.Single();

                    var binding = Binding.GetBinding(method);
                    if (binding != null)
                    {
                        var function = new Function(_context, method, binding);

                        function.CheckSignature();

                        Functions.Add(function);
                    }
                }
            }
            else
            {
                // fast path, you don't enter this without having 
                // gone through the other path at least once before
                // allthough not necessarily within the same process

                MethodInfo method = null;

                try
                {
                    method = _context.GetType().GetMethod(
                       name,
                       BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                   );
                }
                catch (AmbiguousMatchException ex)
                {
                    throw new InvalidOperationException($"overloading is not supported. (you cannot have two methods with the same name '{name}')", ex);
                }

                if (method == null)
                {
                    throw new MissingMethodException(_context.GetType().FullName, name);
                }

                var binding = Binding.GetBinding(method);
                if (binding != null)
                {
                    Functions.Add(new Function(_context, method, binding));
                }
            }
        }
    }
}
