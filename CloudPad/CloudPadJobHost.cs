using CloudPad.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CloudPad
{
    class CloudPadReflectionHttpMessageHandler : HttpMessageHandler
    {
        private readonly object _target;
        private readonly MethodInfo _handler;
        private readonly ParameterInfo[] _parameters;

        public CloudPadReflectionHttpMessageHandler(object target, MethodInfo handler, ParameterInfo[] parameters)
        {
            _target = target;
            _handler = handler;
            _parameters = parameters;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object returnValue;

            switch (_parameters.Length)
            {
                case 1:
                    returnValue = _handler.Invoke(_target, new object[] { request });
                    break;

                case 2:
                    returnValue = _handler.Invoke(_target, new object[] { request, cancellationToken });
                    break;

                default:
                    throw new InvalidOperationException();
            }

            var task = returnValue as Task<HttpResponseMessage>;
            if (task != null)
            {
                return await task;
            }

            var res = returnValue as HttpResponseMessage;
            if (res != null)
            {
                return res;
            }

            throw new InvalidOperationException();
        }
    }

    public class CloudPadJobHost : IDisposable
    {
        private readonly HttpServer _httpListener;
        private readonly object _context;
        private readonly string _methodName;
        private readonly string[] _args;

        public CloudPadJobHost(object context, string[] args)
        {
            _httpListener = new HttpServer();
            _context = context;

            // no args, default bindings
            // any args, no default bindings

            if (1 <= (args?.Length ?? 0))
            {
                _methodName = args[0];
                _args = new ArraySegment<string>(args, 1, args.Length - 1).ToArray();
            }
            else
            {
                var methodNameSet = new HashSet<string>(StringComparer.Ordinal);

                var methods = context.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var m in methods)
                {
                    var binding = Binding.GetBinding(m);
                    if (binding == null)
                    {
                        continue;
                    }

                    if (methodNameSet.Add(m.Name))
                    {
                        RegisterBinding(binding);
                    }
                    else
                    {
                        throw new InvalidOperationException($"method '{m.Name}' is overloaded, you must give each method a unique name");
                    }
                }
            }
        }

        private void RegisterBinding(Binding binding)
        {
            if (binding.Type == BindingType.HttpTrigger)
            {
                // check method signature

                var parameters = binding.Method.GetParameters();
                switch (parameters.Length)
                {
                    case 1:
                    case 2:
                        break;

                    default:
                        //LINQPad.Extensions.Dump($"method {m.Name} does not match HTTP handler signature (rank)");
                        return;
                }

                if (!typeof(HttpRequestMessage).IsAssignableFrom(parameters[0].ParameterType))
                {
                    //LINQPad.Extensions.Dump($"method {m.Name} does not match HTTP handler signature (parameter #1 type)");
                    return;
                }

                if (1 < parameters.Length)
                {
                    if (!typeof(CancellationToken).IsAssignableFrom(parameters[1].ParameterType))
                    {
                        //LINQPad.Extensions.Dump($"method {m.Name} does not match HTTP handler signature (parameter #2 type)");
                        return;
                    }
                }

                if (!(typeof(Task<HttpResponseMessage>).IsAssignableFrom(binding.Method.ReturnType) ||
                    typeof(HttpResponseMessage).IsAssignableFrom(binding.Method.ReturnType)))
                {
                    //LINQPad.Extensions.Dump($"method {m.Name} does not match HTTP handler signature (return type)");
                    return;
                }

                // todo: create delegate

                _httpListener.Configuration.Routes.MapHttpRoute(binding.Route, binding.Route, null, null, new CloudPadReflectionHttpMessageHandler(_context, binding.Method, parameters));
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(_methodName))
            {
                await RunAsync();
            }
            else
            {
                //Debugger.Launch();

                var binding = Binding.GetBinding(_context.GetType(), _methodName);
                if (binding == null)
                {
                    throw new InvalidOperationException(FormattableString.Invariant($"method {_methodName} does not have binding"));
                }

                RegisterBinding(binding);

                switch (binding.Type)
                {
                    case BindingType.HttpTrigger:
                        {
                            var reqFileName = _args[0];
                            var resFileName = _args[1];

                            HttpRequestMessage req;

                            using (var inputStream = File.OpenRead(reqFileName))
                            {
                                req = HttpMessage.DeserializeRequest(inputStream);
                            }

                            var res = await _httpListener.ProcessAsync(req, cancellationToken);

                            using (var outputStream = File.Create(resFileName))
                            {
                                await HttpMessage.SerializeResponse(res, outputStream);
                            }
                        }
                        break;

                    case BindingType.TimerTrigger:
                        var returnValue = binding.Method.Invoke(_context, null); // parameterless  
                        if (returnValue is Task)
                        {
                            await (Task)returnValue;
                        }
                        return;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private async Task RunAsync()
        {
            var cts = new CancellationTokenSource();

            var utilType = Type.GetType("LINQPad.Util, LINQPad", false);
            if (utilType != null)
            {
                utilType.GetEvent("Cleanup").AddEventHandler(null, new EventHandler((sender, e) => cts.Cancel()));
            }

            await _httpListener.RunAsync(cts.Token);
        }

        public void Dispose()
        {
            _httpListener.Dispose();
        }
    }
}
