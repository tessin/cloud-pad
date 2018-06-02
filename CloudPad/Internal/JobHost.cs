using NCrontab;
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

namespace CloudPad.Internal
{
    public class JobHost : IDisposable
    {
        private readonly object _context;
        private readonly string[] _args;

        public JobHost(object context, string[] args)
        {
            //#if DEBUG
            //            Debugger.Launch();
            //#endif

            _context = context;
            _args = args;
        }

        public async Task<int> WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var args = new Args(Options.Method);
            if (args.Parse(_args))
            {
                Log.Trace.Append("cannot parse command line");
                return 2;
            }

            var method = args.GetSingleOrDefault(Options.Method);

            var index = new FunctionIndex(_context);

            index.Initialize(method);

            using (var cts = new CancellationScope(cancellationToken))
            {
                // todo: compilation

                if (string.IsNullOrEmpty(method))
                {
                    await RunAsync(index.Functions, cts.Token);
                }
                else
                {
                    // specific function invocation

                    var function = index.Functions.FirstOrDefault(x => x.Name == method);
                    if (function == null)
                    {
                        Log.Trace.Append($"function '{method}' not found");
                        return 1;
                    }

                    switch (function.Binding.Type)
                    {
                        case BindingType.HttpTrigger:
                            {
                                var reqFileName = args.GetSingleOrDefault(Options.RequestFileName);
                                if (reqFileName == null)
                                {
                                    Log.Trace.Append($"HTTP function '{method}' requires -req <request-file-name> option");
                                    return 1;
                                }

                                var resFileName = args.GetSingleOrDefault(Options.ResponseFileName);
                                if (resFileName == null)
                                {
                                    Log.Trace.Append($"HTTP function '{method}' requires -res <response-file-name> option");
                                    return 1;
                                }

                                using (var httpServer = new HttpServer())
                                {
                                    httpServer.RegisterFunction(function);

                                    // invoke HTTP function

                                    HttpRequestMessage req;

                                    using (var inputStream = File.OpenRead(reqFileName))
                                    {
                                        req = HttpMessage.DeserializeRequest(inputStream);
                                    }

                                    var res = await httpServer.InvokeAsync(req, cancellationToken);

                                    using (var outputStream = File.Create(resFileName))
                                    {
                                        await HttpMessage.SerializeResponse(res, outputStream);
                                    }
                                }
                                break;
                            }

                        case BindingType.TimerTrigger:
                            {
                                using (var timerServer = new TimerServer())
                                {
                                    timerServer.RegisterFunction(function);

                                    // invoke timer function

                                    await timerServer.InvokeAsync(method, cancellationToken);
                                }
                                break;
                            }

                        default:
                            {
                                Log.Trace.Append($"function '{method}' binding '{function.Binding.Type}' is unsupported");
                                return 1;
                            }
                    }
                }
            }

            return 0;
        }

        private async Task RunAsync(IEnumerable<Function> functions, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var httpServer = new HttpServer())
            {
                using (var timerServer = new TimerServer())
                {
                    foreach (var function in functions)
                    {
                        switch (function.Binding.Type)
                        {
                            case BindingType.HttpTrigger:
                                {
                                    httpServer.RegisterFunction(function);
                                    break;
                                }
                            case BindingType.TimerTrigger:
                                {
                                    timerServer.RegisterFunction(function);
                                    break;
                                }
                            default:
                                throw new InvalidOperationException($"binding type: {function.Binding.Type}");
                        }
                    }

                    var httpServerTask = httpServer.RunAsync(cancellationToken);
                    var timerServerTask = timerServer.RunAsync(cancellationToken);

                    var tasks = new List<Task>();

                    tasks.Add(httpServerTask);
                    tasks.Add(timerServerTask);

                    while (0 < tasks.Count)
                    {
                        var task = await Task.WhenAny(tasks);
                        await task; // unwrap
                        tasks.Remove(task);
                    }
                }
            }
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

                // todo: wash LINQPad script 
                // - fix assembly references
                // - fix connection string

                var baseName = Path.GetFileNameWithoutExtension(linqPadScriptFileName);
                var fn = "scripts/" + baseName + "/" + Path.GetFileName(linqPadScriptFileName);

                zip.CreateEntryFromFile(linqPadScriptFileName, fn);

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

                    functionJson["linqPadScriptFileName"] = "../" + fn;
                    functionJson["linqPadScriptMethodName"] = binding.GetMethodName();

                    var entry = zip.CreateEntry(baseName + "_" + binding.GetMethodName() + "/function.json");

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
            // no op
        }
    }
}
