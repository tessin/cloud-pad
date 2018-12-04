using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public class Invoker : IDisposable
    {
        private DuplexPipe _serverPipe;
        private Process _childProcess;
        private ConcurrentDictionary<Guid, TaskCompletionSource<Result>> _outstanding;
        private Task _serverTask;

        private async Task ServerAsync()
        {
            var serverPipe = _serverPipe;
            var outstanding = _outstanding;

            Log.Debug.Append($"running");

            for (; ; )
            {
                var msg = await serverPipe.InPipe.ReceiveAsync<Result>();
                if (msg == null)
                {
                    return; // lprun stopped, crashed or otherwise exited
                }

                Log.Debug.Append($"received {nameof(msg.CorrelationId)}={msg.CorrelationId}");

                if (outstanding.TryRemove(msg.CorrelationId, out var tcs))
                {
                    tcs.TrySetResult(msg);
                }
            }
        }

        private async Task<Result> RunAsync(string linqPadScriptFileName, string[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            TextWriter lpRunLog = null;

            lock (this)
            {
                // is LINQPad server running?
                if (_serverTask == null)
                {
                    Log.Debug.Append($"LINQPad proxy server is not running");

                    var serverPipe = new DuplexPipe();

                    var processStartInfo = new ProcessStartInfo { UseShellExecute = false };

                    var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REGION_NAME"));

                    Log.Debug.Append($"{nameof(isAzure)}={isAzure}");

                    if (isAzure)
                    {
                        // todo: Environment.GetEnvironmentVariable("CLOUD_PAD_LINQ_PAD_VERSION"); ?

                        var linqPadDirectory = Directory.EnumerateDirectories(@"D:\home\site\tools", "LINQPad.*").LastOrDefault();
                        if (linqPadDirectory == null)
                        {
                            throw new Exception(@"LINQPad is not installed in location 'D:\home\site\tools\LINQPad.*' on host");
                        }

                        processStartInfo.FileName = Path.Combine(linqPadDirectory, "LPRun.exe");
                        processStartInfo.WorkingDirectory = @"D:\home\site\wwwroot\bin";

                        // retention policy: last 10 log files

                        foreach (var fn in Directory.EnumerateFiles(@"D:\home\LogFiles\Application", "LPRun_*.log").OrderBy(fn => fn).Skip(10))
                        {
                            File.Delete(fn);
                        }

                        // by having the PID in the file name we prevent filename clashes

                        lpRunLog = File.AppendText($@"D:\home\LogFiles\Application\LPRun_{DateTime.UtcNow:yyyyMMdd_HHmm}_{Process.GetCurrentProcess().Id}.log");
                    }
                    else
                    {
                        processStartInfo.FileName = @"C:\Program Files (x86)\LINQPad5\LPRun.exe";

                        // depends on hosting environment, test or function app
                        var currentDirectory = Environment.CurrentDirectory;
                        var binDirectory = Path.Combine(currentDirectory, "bin");

                        if (Directory.Exists(binDirectory))
                        {
                            processStartInfo.WorkingDirectory = binDirectory;
                        }
                        else
                        {
                            processStartInfo.WorkingDirectory = currentDirectory;
                        }
                    }

                    {
                        // deploy proxy.linq

                        var proxyScript = this.GetType().Assembly.GetManifestResourceStream("CloudPad.proxy.linq");
                        var proxyScriptDigest = BitConverter.ToString(new SHA256Managed().ComputeHash(proxyScript)).Replace("-", "").Substring(0, 32).ToLowerInvariant();
                        var proxyScriptFileName = "proxy_" + proxyScriptDigest + ".linq";

                        var fn = Path.Combine(processStartInfo.WorkingDirectory, proxyScriptFileName);
                        if (!File.Exists(fn))
                        {
                            proxyScript.Position = 0;
                            using (var fs = File.Create(fn))
                            {
                                proxyScript.CopyTo(fs);
                            }
                        }

#if DEBUG
                        processStartInfo.Arguments = proxyScriptFileName + " " + serverPipe.GetClientHandleAsString();
#else
                        processStartInfo.Arguments = "-optimize" + " " + proxyScriptFileName + " " + serverPipe.GetClientHandleAsString();
#endif
                    }

                    processStartInfo.RedirectStandardOutput = true;
                    processStartInfo.RedirectStandardError = true;

                    var childProcess = Process.Start(processStartInfo);

                    childProcess.EnableRaisingEvents = true;

                    Debug.WriteLine("LPRun started");

                    if (lpRunLog != null)
                    {
                        lock (lpRunLog)
                        {
                            lpRunLog.WriteLine($"LPRun started {DateTime.UtcNow:o}");
                        }
                    }

                    childProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            Debug.WriteLine(e.Data, "stdout");

                            if (lpRunLog != null)
                            {
                                lock (lpRunLog)
                                {
                                    lpRunLog.WriteLine(e.Data);
                                }
                            }
                        }
                    };
                    childProcess.BeginOutputReadLine();

                    childProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            Debug.WriteLine(e.Data, "stderr");

                            if (lpRunLog != null)
                            {
                                lock (lpRunLog)
                                {
                                    lpRunLog.WriteLine(e.Data);
                                }
                            }
                        }
                    };
                    childProcess.BeginErrorReadLine();

                    childProcess.Exited += (sender, e) =>
                    {
                        Debug.WriteLine($"LINQPad exited");

                        if (lpRunLog != null)
                        {
                            var lpRunLog2 = lpRunLog;
                            lock (lpRunLog2)
                            {
                                lpRunLog2.WriteLine($"LPRun exited {DateTime.UtcNow:o} with exit code {childProcess.ExitCode}");
                                lpRunLog2.Flush();
                                lpRunLog2.Dispose();
                                lpRunLog = null; // no more logging to this stream
                            }
                        }

                        // todo: abort pending tasks

                        _serverPipe.Dispose();
                        _childProcess.Dispose();
                        foreach (var item in _outstanding)
                        {
                            item.Value.TrySetCanceled();
                        }
                        _outstanding.Clear();
                        _serverTask = null;
                    };

                    serverPipe.DisposeLocalCopyOfClientHandle();

                    _serverPipe = serverPipe;
                    _childProcess = childProcess;
                    _outstanding = new ConcurrentDictionary<Guid, TaskCompletionSource<Result>>(); // todo: multi message channel
                    _serverTask = Task.Factory.StartNew(() => ServerAsync()).Unwrap();
                }
            }

            var tcs = new TaskCompletionSource<Result>();

            var envelope = Envelope.Create(linqPadScriptFileName, args);

            if (!_outstanding.TryAdd(envelope.CorrelationId, tcs))
            {
                throw new InvalidOperationException();
            }

            Log.Debug.Append($"sending {nameof(envelope.CorrelationId)}={envelope.CorrelationId}");

            if (lpRunLog != null)
            {
                lock (lpRunLog)
                {
                    lpRunLog.WriteLine($"LINQPad script '{envelope.LINQPadScriptFileName}' started {DateTime.UtcNow:o}");
                }
            }

            _serverPipe.OutPipe.Send(envelope);

            using (cancellationToken.Register(() =>
            {
                if (_outstanding.TryRemove(envelope.CorrelationId, out var tcs2))
                {
                    tcs2.TrySetCanceled();
                }
            }))
            {
                try
                {
                    var result = await tcs.Task;

                    if (lpRunLog != null)
                    {
                        lock (lpRunLog)
                        {
                            lpRunLog.WriteLine($"LINQPad script '{envelope.LINQPadScriptFileName}' exited {DateTime.UtcNow:o} ErrorCode={result.ErrorCode}");

                            if (!string.IsNullOrEmpty(result.ExceptionTypeFullName))
                            {
                                lpRunLog.WriteLine($"ExceptionTypeFullName: {result.ExceptionTypeFullName}");
                            }

                            if (!string.IsNullOrEmpty(result.ExceptionMessage))
                            {
                                lpRunLog.WriteLine($"ExceptionMessage     : {result.ExceptionMessage}");
                            }

                            if (!string.IsNullOrEmpty(result.ExceptionFusionLog))
                            {
                                lpRunLog.WriteLine($"ExceptionFusionLog   : {result.ExceptionFusionLog}");
                            }

                            if (!string.IsNullOrEmpty(result.ExceptionStackTrace))
                            {
                                lpRunLog.WriteLine($"ExceptionStackTrace  : {result.ExceptionStackTrace}");
                            }
                        }
                    }

                    return result;
                }
                finally
                {
                    if (lpRunLog != null)
                    {
                        lock (lpRunLog)
                        {
                            lpRunLog.Flush(); // flush after script invocation
                        }
                    }
                }
            }
        }

        public async Task<HttpResponseMessage> RunHttpTriggerAsync(
            string linqPadScriptFileName,
            string methodName,
            HttpRequestMessage req,
            ITraceWriter log,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var temp = Path.GetTempPath();

            var reqFn = Path.Combine(temp, FormattableString.Invariant($"{Guid.NewGuid()}.req"));
            var resFn = Path.Combine(temp, FormattableString.Invariant($"{Guid.NewGuid()}.res"));

            try
            {
                using (var outputStream = File.Create(reqFn))
                {
                    await HttpMessage.SerializeRequest(req, outputStream);
                }

                Log.Debug.Append($"request written to file {reqFn}");

                var result = await RunAsync(
                    linqPadScriptFileName,
                    new[] { "-" + Options.Method, methodName, "-" + Options.RequestFileName, reqFn, "-" + Options.ResponseFileName, resFn },
                    cancellationToken
                );

                HandleResult(result);

                using (var inputStream = File.OpenRead(resFn))
                {
                    var res = HttpMessage.DeserializeResponse(inputStream);

                    // if we do not associate the response with the original request
                    // the Azure Web Jobs SDK will ignore our actual response message
                    // and simply return a generic HTTP 200 OK response message, evil!

                    // see https://github.com/Azure/azure-functions-host/blob/v1.x/src/WebJobs.Script.WebHost/WebScriptHostManager.cs#L211-L217
                    // see https://github.com/Azure/azure-functions-host/blob/v1.x/src/WebJobs.Script/ScriptConstants.cs#L11

                    req.Properties.Add("MS_AzureFunctionsHttpResponse", res);

                    return res;
                }
            }
            catch (Exception ex)
            {
                var res = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                res.Content = new StringContent(ex.Message);
                return res;
            }
            finally
            {
                File.Delete(reqFn);
                File.Delete(resFn);
            }
        }

        public async Task RunTimerTriggerAsync(
            string linqPadScriptFileName,
            string methodName,
            ITraceWriter log,
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var result = await RunAsync(
                linqPadScriptFileName,
                new[] { "-" + Options.Method, methodName },
                cancellationToken
            );

            HandleResult(result);
        }

        private void HandleResult(Result result)
        {
            if (result.ErrorCode == 0)
            {
                Log.Debug.Append(result.Text); // OK
            }
            else
            {
                throw new CloudPadException("remote script execution failed",
                    remoteTypeFullName: result.ExceptionTypeFullName,
                    remoteMessage: result.ExceptionMessage,
                    remoteStackTrace: result.ExceptionStackTrace
                );
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                var childProcess = _childProcess;
                if (childProcess != null)
                {
                    try
                    {
                        childProcess.Kill();
                    }
                    catch
                    {
                        // nom nom nom...
                    }
                    finally
                    {
                        childProcess.Dispose();
                    }
                }

                var serverPipe = _serverPipe;
                if (serverPipe != null)
                {
                    serverPipe.Dispose();
                }
            }
        }
    }
}
