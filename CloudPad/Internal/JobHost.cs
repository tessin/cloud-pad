using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }
            _context = context;
            _args = args;
        }

        public async Task<int> WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var args = new Args(Options.Script, Options.Method, Options.RequestFileName, Options.ResponseFileName, Options.Compile + ":boolean", Options.OutputDirectory, Options.Debug + ":boolean", Options.Publish, Options.Unpublish);
            if (!args.Parse(_args))
            {
                Log.Trace.Append("cannot parse command line: " + string.Join(" ", _args));
                return 2;
            }

            var script = args.GetSingleOrDefault(Options.Script);
            var method = args.GetSingleOrDefault(Options.Method);
            var compile = args.GetSingleOrDefault<bool>(Options.Compile);
            var outputDirectory = args.GetSingleOrDefault(Options.OutputDirectory);
            var debug = args.GetSingleOrDefault<bool>(Options.Debug);
            var publish = args.GetSingleOrDefault(Options.Publish);
            var unpublish = args.GetSingleOrDefault(Options.Unpublish);

            if (debug)
            {
                Debugger.Launch();
            }

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
                    if (script == null)
                    {
                        script = LINQPad.GetCurrentQueryPath();
                    }

                    if (script == null)
                    {
                        Console.Error.WriteLine($"'{Options.Compile}' depends on '{Options.Script}' when running outside of LINQPad context");
                        return 1;
                    }

                    script = Path.GetFullPath(script); // fully qualified

                    await CompileAsync(script, index.Functions, outputDirectory, publish);
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
            string scriptFullPath,
            IEnumerable<Function> functions,
            string outputDirectory = null,
            string publish = null
        )
        {
            // todo: packaging...

            var assembly = GetType().Assembly;
            var assemblyName = assembly.GetName();

            var zipFileName = Path.ChangeExtension(Path.GetFileName(scriptFullPath), "zip");

            using (var fs = File.Create(zipFileName))
            {
                var zip = new ZipArchive(fs, ZipArchiveMode.Create);

                var scriptDirectory = Path.GetDirectoryName(scriptFullPath);
                var scriptFileName = Path.GetFileName(scriptFullPath);
                var scriptBaseName = Path.GetFileNameWithoutExtension(scriptFullPath);

                var baseName = "scripts" + "/" + scriptBaseName;

                // ================================

                var f = new LINQPadFile();
                f.Load(scriptFullPath);
                foreach (var el in f.metadata_.Elements("Reference"))
                {
                    var value = el.Value;
                    if (value.StartsWith(@"<RuntimeDirectory>\"))
                    {
                        continue;
                    }

                    // locate DLL

                    string dllFullPath = null;

                    if (File.Exists(value))
                    {
                        dllFullPath = value;
                    }
                    else
                    {
                        var rel = (string)el.Attribute("Relative"); // todo: what happens if rel is null?
                        var abs = Path.GetFullPath(Path.Combine(scriptDirectory, rel));

                        if (File.Exists(abs))
                        {
                            dllFullPath = abs;
                        }
                    }

                    if (dllFullPath == null)
                    {
                        FormattableString message = $"compiler error: cannot resolve LINQPad script '{scriptFullPath}' assembly reference '{value}'";
                        Log.Trace.Append(message);
                        throw new CloudPadException(FormattableString.Invariant(message));
                    }

                    var dllFileName = Path.GetFileName(dllFullPath);

                    el.Value = Path.Combine(@"D:\home\site\wwwroot\scripts", scriptBaseName, "bin", dllFileName); // rewrite
                    el.SetAttributeValue("Relative", Path.Combine("bin", dllFileName));

                    zip.CreateEntryFromFile(dllFullPath, baseName + "/" + "bin" + "/" + dllFileName);

                    var pdbFullPath = Path.ChangeExtension(dllFullPath, "pdb");

                    if (File.Exists(pdbFullPath))
                    {
                        zip.CreateEntryFromFile(pdbFullPath, baseName + "/" + "bin" + "/" + Path.GetFileName(pdbFullPath));
                    }

                    // ================================

                    var attachmentsPath = ProbePath($"{scriptBaseName}.files.txt", new[] { scriptDirectory, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                    if (attachmentsPath != null)
                    {
                        foreach (var line in File.ReadAllLines(attachmentsPath))
                        {
                            var line2 = line.Trim();
                            if (string.IsNullOrEmpty(line2))
                            {
                                continue;
                            }
                            var attachmentPath = ProbePath(line2, new[] { scriptDirectory, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                            if (attachmentPath == null)
                            {
                                Console.Error.WriteLine($"'{line2}' script attachment not found");
                                if (Environment.UserInteractive)
                                {
                                    Console.ReadLine();
                                }
                                Environment.Exit(1);
                                return;
                            }
                            zip.CreateEntryFromFile(attachmentPath, baseName + "/" + Path.GetFileName(attachmentPath));
                        }
                    }

                    var attachmentsPath2 = ProbePath("files.txt", new[] { scriptDirectory, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                    if (attachmentsPath2 != null)
                    {
                        foreach (var line in File.ReadAllLines(attachmentsPath))
                        {
                            var line2 = line.Trim();
                            if (string.IsNullOrEmpty(line2))
                            {
                                continue;
                            }
                            var attachmentPath = ProbePath(line2, new[] { scriptDirectory, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                            if (attachmentPath == null)
                            {
                                Console.Error.WriteLine($"'{line2}' global attachment not found");
                                if (Environment.UserInteractive)
                                {
                                    Console.ReadLine();
                                }
                                Environment.Exit(1);
                                return;
                            }
                            zip.CreateEntryFromFile(attachmentPath, baseName + "/" + Path.GetFileName(attachmentPath));
                        }
                    }

                    // ================================
                }

                f.PrepareConnection();

                zip.CreateEntryFromLINQPadFile(f, baseName + "/" + scriptFileName);

                // ================================

                foreach (var function in functions)
                {
                    var functionJson = new JObject();

                    functionJson["generatedBy"] = assemblyName.Name + "-" + assemblyName.Version;

                    // "attributes" is used by the Azure Web Jobs SDK 
                    // to bind using metadata and takes precedence 
                    // over "config". we do not want this.
                    functionJson["configurationSource"] = "config";

                    functionJson["bindings"] = JToken.FromObject(new[] { function.Binding });
                    functionJson["disabled"] = false;
                    functionJson["scriptFile"] = "../bin/CloudPad.FunctionApp.dll";

                    switch (function.Binding.Type)
                    {
                        case BindingType.HttpTrigger:
                            functionJson["entryPoint"] = "CloudPad.HttpFunctionEntryPoint.Run";
                            break;

                        case BindingType.TimerTrigger:
                            functionJson["entryPoint"] = "CloudPad.TimerFunctionEntryPoint.Run";
                            break;

                        default:
                            throw new NotSupportedException("trigger binding: " + function.Binding.Type);
                    }

                    functionJson["linqPadScriptFileName"] = "../scripts/" + scriptBaseName + "/" + scriptFileName; // relative 'function.json'
                    functionJson["linqPadScriptMethodName"] = function.Name;

                    zip.CreateEntryFromJson(functionJson, scriptBaseName + "_" + function.Name + "/function.json");
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

        static string ProbePath(string fn, string[] paths)
        {
            foreach (var path in paths)
            {
                var fn2 = Path.Combine(path, fn);
                if (File.Exists(fn2))
                {
                    return fn2;
                }
            }
            return null;
        }

        public void Dispose()
        {
            // no op
        }
    }
}
