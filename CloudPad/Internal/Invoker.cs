using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public class Invoker : IDisposable
    {
        private AnonymousPipeServerStream _request;
        private AnonymousPipeServerStream _response;
        private Process _childProcess;
        private ConcurrentDictionary<Guid, TaskCompletionSource<Result>> _outstanding;
        private Task _serverTask;

        private async Task ServerAsync()
        {
            var response = _response;
            var outstanding = _outstanding;

            Log.Debug.Append($"running");

            for (; ; )
            {
                var msg = await response.ReceiveAsync<Result>();
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

        private async Task<Result> RunAsync(string linqPadScriptFileName, string methodName, string[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (this)
            {
                // is LINQPad server running?
                if (_serverTask == null)
                {
                    Log.Debug.Append($"LINQPad proxy server is not running");

                    var request = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                    var response = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

                    var processStartInfo = new ProcessStartInfo { UseShellExecute = false };

                    // if Azure these need to be changed
                    var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REGION_NAME"));

                    Log.Debug.Append($"{nameof(isAzure)}={isAzure}");

                    if (isAzure)
                    {
                        // todo:
                        // Environment.GetEnvironmentVariable("CLOUD_PAD_LINQ_PAD_VERSION");

                        var linqPadDirectory = Directory.EnumerateDirectories(@"D:\home\site\tools", "LINQPad.*").LastOrDefault();
                        if (linqPadDirectory == null)
                        {
                            throw new Exception("LINQPad is not installed on host");
                        }

                        processStartInfo.FileName = Path.Combine(linqPadDirectory, "LPRun.exe");
                        processStartInfo.WorkingDirectory = @"D:\home\site\wwwroot";
                    }
                    else
                    {
                        processStartInfo.FileName = @"C:\Program Files (x86)\LINQPad5\LPRun.exe";
                        processStartInfo.WorkingDirectory = Environment.CurrentDirectory;
                    }

                    processStartInfo.Arguments = "-optimize" + " " + @"bin\proxy.linq" + " " + request.GetClientHandleAsString() + " " + response.GetClientHandleAsString();

                    // dump Proxy
#if DEBUG
                    processStartInfo.RedirectStandardOutput = true;
                    processStartInfo.RedirectStandardError = true;
#endif

                    var childProcess = Process.Start(processStartInfo);

                    Log.Debug.Append($"LINQPad proxy server started");

#if DEBUG
                    childProcess.OutputDataReceived += (sender, e) => Debug.WriteLine(e.Data, "Output");
                    childProcess.BeginOutputReadLine();

                    childProcess.ErrorDataReceived += (sender, e) => Debug.WriteLine(e.Data, "Error");
                    childProcess.BeginErrorReadLine();
#endif

                    // todo: how to validate the the snippet actually started
                    childProcess.Exited += (sender, e) => {
                        // todo: abort pending tasks, clear serverTask
                    };

                    request.DisposeLocalCopyOfClientHandle();
                    response.DisposeLocalCopyOfClientHandle();

                    _request = request;
                    _response = response;
                    _childProcess = childProcess;
                    _outstanding = new ConcurrentDictionary<Guid, TaskCompletionSource<Result>>(); // todo: multi message channel
                    _serverTask = Task.Factory.StartNew(() => ServerAsync()).Unwrap();
                }
            }

            var tcs = new TaskCompletionSource<Result>();

            var envelope = Envelope.Create(linqPadScriptFileName, methodName, args);

            if (!_outstanding.TryAdd(envelope.CorrelationId, tcs))
            {
                throw new InvalidOperationException();
            }

            Log.Debug.Append($"sending {nameof(envelope.CorrelationId)}={envelope.CorrelationId}");

            _request.Send(envelope);

            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }

        public async Task<HttpResponseMessage> RunHttpTriggerAsync(
            string linqPadScriptFileName,
            string methodName,
            HttpRequestMessage req,
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
                    methodName,
                    new[] { reqFn, resFn },
                    cancellationToken
                );

                HandleResult(result);

                using (var inputStream = File.OpenRead(resFn))
                {
                    return HttpMessage.DeserializeResponse(inputStream);
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
            CancellationToken cancellationToken = default(CancellationToken)
            )
        {
            var result = await RunAsync(
                linqPadScriptFileName,
                methodName,
                null,
                cancellationToken
            );

            HandleResult(result);
        }

        private void HandleResult(Result result)
        {
            // todo: check for error

            Log.Debug.Append(result.Text);
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
            }
        }
    }
}
