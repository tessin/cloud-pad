using CloudPad.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
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
                RegisterBindings(context);
            }
        }

        private List<Binding> RegisterBindings(object context)
        {
            var bindings = new List<Binding>();

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
                    if (!RegisterBinding(binding))
                    {
                        return null;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"method '{m.Name}' is overloaded, you must give each method a unique name");
                }

                bindings.Add(binding);
            }

            return bindings;
        }

        private bool RegisterBinding(Binding binding)
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
                        Log.Trace.Append($"method {binding.Method.Name} does not match HTTP handler signature (rank)");
                        return false;
                }

                if (!typeof(HttpRequestMessage).IsAssignableFrom(parameters[0].ParameterType))
                {
                    Log.Trace.Append($"method {binding.Method.Name} does not match HTTP handler signature (parameter #1 type)");
                    return false;
                }

                if (1 < parameters.Length)
                {
                    if (!typeof(CancellationToken).IsAssignableFrom(parameters[1].ParameterType))
                    {
                        Log.Trace.Append($"method {binding.Method.Name} does not match HTTP handler signature (parameter #2 type)");
                        return false;
                    }
                }

                if (!(typeof(Task<HttpResponseMessage>).IsAssignableFrom(binding.Method.ReturnType) ||
                    typeof(HttpResponseMessage).IsAssignableFrom(binding.Method.ReturnType)))
                {
                    Log.Trace.Append($"method {binding.Method.Name} does not match HTTP handler signature (return type)");
                    return false;
                }

                // todo: create delegate

                _httpListener.Configuration.Routes.MapHttpRoute(binding.Route, "api/" + binding.Route, null, null, new CloudPadReflectionHttpMessageHandler(_context, binding.Method, parameters));
                return true;
            }

            return false;
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(_methodName))
            {
                await RunAsync();
            }
            else
            {
                if (_methodName == "-compile")
                {
                    await CompileAsync();
                    return;
                }

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

            // subscribe to LINQPad script cancellation
            var utilType = Type.GetType("LINQPad.Util, LINQPad", false);
            if (utilType != null)
            {
                utilType.GetEvent("Cleanup").AddEventHandler(null, new EventHandler((sender, e) => cts.Cancel()));
            }

            await _httpListener.RunAsync(cts.Token);
        }

        private async Task CompileAsync()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            const string OutputDirectory = "out";

            var args = new Args(OutputDirectory);
            if (!args.Parse(_args))
            {
                Environment.Exit(2);
                return;
            }

            // ================

            var utilType = Type.GetType("LINQPad.Util, LINQPad", false);
            if (utilType == null)
            {
                Log.Trace.Append("cannot find LINQPad Util type");
                Environment.Exit(1);
                return;
            }

            var linqPadScriptFileName = utilType.GetProperty("CurrentQueryPath")?.GetValue(null) as string;

            if (string.IsNullOrEmpty(linqPadScriptFileName))
            {
                Log.Trace.Append("cannot find LINQPad script file name");
                Environment.Exit(1);
                return;
            }

            var bindings = RegisterBindings(_context);
            if (bindings == null)
            {
                // error
                Log.Trace.Append("one or more bindings has errors");
                Environment.Exit(1);
                return;
            }

            var assembly = GetType().Assembly;
            var assemblyName = assembly.GetName();

            var zipFileName = Path.ChangeExtension(Path.GetFileName(linqPadScriptFileName), "zip");

            using (var fs = File.Create(zipFileName))
            {
                var zip = new ZipArchive(fs, ZipArchiveMode.Create);

                // todo: wash LINQPad script (fix broken assembly references)

                zip.CreateEntryFromFile(linqPadScriptFileName, "scripts/" + Path.GetFileName(linqPadScriptFileName));

                foreach (var binding in bindings)
                {
                    var functionJson = new JObject();

                    functionJson["generatedBy"] = assemblyName.Name + "-" + assemblyName.Version;
                    functionJson["bindings"] = JToken.FromObject(new[] { binding });
                    functionJson["disabled"] = false;
                    functionJson["scriptFile"] = "../bin/CloudPadFunctionHost.dll";

                    switch (binding.Type)
                    {
                        case BindingType.HttpTrigger:
                            functionJson["entryPoint"] = "CloudPadFunctionHost.HttpFunction.Run";
                            break;

                        default:
                            throw new NotSupportedException("trigger binding: " + binding.Type);
                    }

                    functionJson["linqPadScriptFileName"] = "../scripts/" + Path.GetFileName(linqPadScriptFileName);
                    functionJson["linqPadScriptMethodName"] = binding.GetMethodName();

                    var entry = zip.CreateEntry(binding.GetMethodName() + "/function.json");

                    using (var entryStream = entry.Open())
                    {
                        var textWriter = new StreamWriter(entryStream);
                        var jsonSerializer = new JsonSerializer
                        {
                            Formatting = Formatting.Indented
                        };
                        jsonSerializer.Serialize(new JsonTextWriter(textWriter), functionJson);
                        textWriter.Flush();
                    }
                }

                // proxy.linq
                // todo: wash LINQPad script (fix broken assembly references)

                // proxy should probably be deployed as runtime not as functions, 
                // if you want to deploy multiple scripts to the same Azure function host

                {
                    var entry = zip.CreateEntry("bin/proxy.linq");

                    using (var entryStream = entry.Open())
                    {
                        assembly.GetManifestResourceStream("CloudPad.proxy.linq").CopyTo(entryStream);
                    }
                }

                zip.Dispose();
            }

            if (args.Has(OutputDirectory))
            {
                Log.Debug.Append(args.GetSingle(OutputDirectory));
                var outputDirectory = Path.GetFullPath(args.GetSingle(OutputDirectory));
                Log.Debug.Append($"extracting '{outputDirectory}'...");
                using (var inputStream = File.Open(zipFileName, FileMode.Open))
                {
                    var zip = new ZipArchive(inputStream);
                    foreach (var entry in zip.Entries)
                    {
                        var outputFileName = Path.Combine(outputDirectory, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));
                        entry.ExtractToFile(outputFileName, true);
                        Log.Debug.Append($"extracted '{entry.FullName}'");
                    }
                }
            }

            //if (0 < _args.Length && _args[0].StartsWith("publish-profile-"))
            if (false)
            {
                // ok, nice!

                Trace.WriteLine("publishing...");

                var publishProfileFn = Path.GetFullPath(_args[0]);
                var publishProfile = JToken.Parse(File.ReadAllText(publishProfileFn));
                var publishProfile2 = publishProfile["publish-profile"];

                using (var http = new HttpClient())
                {
                    http.BaseAddress = new Uri("https://" + publishProfile2["publishUrl"]);
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(
                            Encoding.ASCII.GetBytes(
                                (string)publishProfile2["userName"] + ":" + (string)publishProfile2["userPWD"]
                            )
                        )
                    );

                    using (var input = File.OpenRead(zipFileName))
                    {
                        var req = new HttpRequestMessage(HttpMethod.Put, "api/zip/" + Uri.EscapeDataString(@"D:\home\site\wwwroot") + "/");
                        req.Content = new StreamContent(input);
                        using (var res = await http.SendAsync(req))
                        {
                            // ok
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _httpListener.Dispose();
        }
    }
}
