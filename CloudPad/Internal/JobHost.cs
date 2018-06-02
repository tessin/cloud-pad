using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            var args = new Args(Options.Method, Options.RequestFileName, Options.ResponseFileName, Options.Compile + ":boolean", Options.OutputDirectory, Options.Publish, Options.Unpublish);
            if (!args.Parse(_args))
            {
                Log.Trace.Append("cannot parse command line: " + string.Join(" ", _args));
                return 2;
            }

            var method = args.GetSingleOrDefault(Options.Method);
            var compile = args.GetSingleOrDefault<bool>(Options.Compile);
            var outputDirectory = args.GetSingleOrDefault(Options.OutputDirectory);
            var publish = args.GetSingleOrDefault(Options.Publish);
            var unpublish = args.GetSingleOrDefault(Options.Unpublish);

            if (compile && unpublish != null)
            {
                Log.Trace.Append($"'{Options.Compile}' and '{Options.Unpublish}' are mutually exclusive");
                return 2;
            }

            if (publish != null && unpublish != null)
            {
                Log.Trace.Append($"'{Options.Publish}' and '{Options.Unpublish}' are mutually exclusive");
                return 2;
            }

            var index = new FunctionIndex(_context);

            index.Initialize(method);

            using (var cts = new CancellationScope(cancellationToken))
            {
                if (compile || publish != null)
                {
                    await CompileAsync(index.Functions, outputDirectory, publish);
                }
                else
                {
                    // todo: unpublish

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

        private async Task CompileAsync(
            IEnumerable<Function> functions,
            string outputDirectory = null,
            string publish = null
        )
        {
            // todo: packaging...

            var linqPadScriptFileName = LINQPad.GetCurrentQueryPath() ?? "test.linq";

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

                foreach (var function in functions)
                {
                    var functionJson = new JObject();

                    functionJson["generatedBy"] = assemblyName.Name + "-" + assemblyName.Version;
                    functionJson["bindings"] = JToken.FromObject(new[] { function.Binding });
                    functionJson["disabled"] = false;
                    functionJson["scriptFile"] = "../bin/CloudPadFunctionHost.dll";

                    switch (function.Binding.Type)
                    {
                        case BindingType.HttpTrigger:
                            functionJson["entryPoint"] = "CloudPadFunctionHost.HttpFunction.Run";
                            break;

                        case BindingType.TimerTrigger:
                            functionJson["entryPoint"] = "CloudPadFunctionHost.TimerFunction.Run";
                            break;

                        default:
                            throw new NotSupportedException("trigger binding: " + function.Binding.Type);
                    }

                    functionJson["linqPadScriptFileName"] = "../" + fn;
                    functionJson["linqPadScriptMethodName"] = function.Name;

                    var entry = zip.CreateEntry(baseName + "_" + function.Name + "/function.json");

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

            if (outputDirectory != null)
            {
                var outputDirectory2 = Path.GetFullPath(outputDirectory);
                Log.Debug.Append($"extracting to '{outputDirectory2}'...");
                using (var inputStream = File.Open(zipFileName, FileMode.Open))
                {
                    var zip = new ZipArchive(inputStream);
                    foreach (var entry in zip.Entries)
                    {
                        var outputFileName = Path.Combine(outputDirectory2, entry.FullName);
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
