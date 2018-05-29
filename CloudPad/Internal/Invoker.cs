using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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

            DebugLog.Append($"running");

            for (; ; )
            {
                var msg = await response.ReceiveAsync<Result>();
                if (msg == null)
                {
                    return; // lprun stopped, crashed or otherwise exited
                }

                DebugLog.Append($"received {nameof(msg.CorrelationId)}={msg.CorrelationId}");

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
                    var request = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                    var response = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

                    var processStartInfo = new ProcessStartInfo { UseShellExecute = false };

                    processStartInfo.FileName = @"C:\Program Files (x86)\LINQPad5\LPRun.exe";
                    processStartInfo.Arguments = "-optimize" + " " + "\"" + @"C:\Users\leidegre\Source\tessin\CloudPad\CloudPad\proxy.linq" + "\"" + " " + request.GetClientHandleAsString() + " " + response.GetClientHandleAsString();

#if DEBUG
                    processStartInfo.RedirectStandardOutput = true;
                    processStartInfo.RedirectStandardError = true;
#endif

                    var childProcess = Process.Start(processStartInfo);

#if DEBUG
                    childProcess.OutputDataReceived += (sender, e) => Debug.WriteLine(e.Data, "Output");
                    childProcess.BeginOutputReadLine();
                    childProcess.ErrorDataReceived += (sender, e) => Debug.WriteLine(e.Data, "Error");
                    childProcess.BeginErrorReadLine();
#endif

                    request.DisposeLocalCopyOfClientHandle();
                    response.DisposeLocalCopyOfClientHandle();

                    _request = request;
                    _response = response;
                    _childProcess = childProcess;
                    _outstanding = new ConcurrentDictionary<Guid, TaskCompletionSource<Result>>();
                    _serverTask = Task.Factory.StartNew(() => ServerAsync()).Unwrap();
                }
            }

            var tcs = new TaskCompletionSource<Result>();

            var envelope = Envelope.Create(linqPadScriptFileName, methodName, args);

            if (!_outstanding.TryAdd(envelope.CorrelationId, tcs))
            {
                throw new InvalidOperationException();
            }

            DebugLog.Append($"sending {nameof(envelope.CorrelationId)}={envelope.CorrelationId}");

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

            DebugLog.Append(result.Text);
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
