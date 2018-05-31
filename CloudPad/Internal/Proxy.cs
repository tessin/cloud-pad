using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public class Proxy : IDisposable
    {
        private readonly Func<string, Task<ILINQPadScript>> _compileAsync;

        private Proxy()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        public Proxy(Func<string, Task<ILINQPadScript>> compileAsync)
            : this()
        {
            _compileAsync = compileAsync;
        }

        public async Task RunAsync(string[] args, CancellationToken cancellationToken)
        {
            if (!(0 < args.Length))
            {
                Log.Trace.Append("missing required command-line argument <request-handle>");
                Environment.Exit(2);
                return;
            }

            if (!(1 < args.Length))
            {
                Log.Trace.Append("missing required command-line argument <response-handle>");
                Environment.Exit(2);
                return;
            }

            using (var duplexPipe = new DuplexPipe(args[0], args[1]))
            {
                //var tasks = new List<Task>();
                //var receiveTask = request.ReceiveAsync();
                //tasks.Add(receiveTask);

                for (; ; )
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    //await Task.WhenAny(tasks);

                    var envelope = await duplexPipe.InPipe.ReceiveAsync<Envelope>();
                    if (envelope == null)
                    {
                        break;
                    }

                    ILINQPadScript compilation;

                    try
                    {
                        compilation = await _compileAsync(envelope.LINQPadScriptFileName);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Debugger.Launch();
                        Log.Debug.Append(
                            $"script compilation error '{envelope.LINQPadScriptFileName}': {ex.Message}",
                            correlationId: envelope.CorrelationId
                        );
#endif
                        var fileNotFound = ex as System.IO.FileNotFoundException;
                        if (fileNotFound != null)
                        {
                            Log.Debug.Append(fileNotFound.FusionLog, correlationId: envelope.CorrelationId);
                        }

                        duplexPipe.OutPipe.Send(new Result
                        {
                            CorrelationId = envelope.CorrelationId,
                            ErrorCode = ResultType.CompileError,
                            ExceptionTypeFullName = ex.GetType().FullName,
                            ExceptionMessage = ex.Message,
                            ExceptionStackTrace = ex.StackTrace,
                            ExceptionFusionLog = fileNotFound?.FusionLog,
                        });

                        continue;
                    }

                    ILINQPadScriptResult result;

                    try
                    {
                        result = await compilation.RunAsync(new[] { envelope.MethodName }.Concat(envelope.Args ?? new string[0]).ToArray());
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Debugger.Launch();
                        Log.Debug.Append(
                            $"script invocation error '{envelope.LINQPadScriptFileName}': {ex.Message}",
                            correlationId: envelope.CorrelationId
                        );
#endif
                        var fileNotFound = ex as System.IO.FileNotFoundException;
                        if (fileNotFound != null)
                        {
                            Log.Debug.Append(fileNotFound.FusionLog, correlationId: envelope.CorrelationId);
                        }

                        duplexPipe.OutPipe.Send(new Result
                        {
                            CorrelationId = envelope.CorrelationId,
                            ErrorCode = ResultType.RunError,
                            ExceptionTypeFullName = ex.GetType().FullName,
                            ExceptionMessage = ex.Message,
                            ExceptionStackTrace = ex.StackTrace,
                            ExceptionFusionLog = fileNotFound?.FusionLog,
                        });

                        continue;
                    }

                    duplexPipe.OutPipe.Send(new Result
                    {
                        CorrelationId = envelope.CorrelationId,
                        Text = await result.GetResultAsync()
                    });
                }
            }
        }

        public void Dispose()
        {
            // clean up, if any
        }
    }
}
