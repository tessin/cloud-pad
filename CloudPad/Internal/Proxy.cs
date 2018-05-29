using System;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public class Proxy : IDisposable
    {
        private readonly Func<string, Task<ILINQPadScript>> _compileAsync;

        public Proxy(Func<string, Task<ILINQPadScript>> compileAsync)
        {
            _compileAsync = compileAsync;
        }

        public async Task RunAsync(string[] args, CancellationToken cancellationToken)
        {
            var requestHandle = args[0];
            var responseHandle = args[1];

            using (var request = new AnonymousPipeClientStream(PipeDirection.In, requestHandle))
            {
                using (var response = new AnonymousPipeClientStream(PipeDirection.Out, responseHandle))
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

                        var envelope = await request.ReceiveAsync<Envelope>();
                        if (envelope == null)
                        {
                            break;
                        }

                        var compilation = await _compileAsync(envelope.LINQPadScriptFileName);

                        var result = await compilation.RunAsync(new[] { envelope.MethodName }.Concat(envelope.Args ?? new string[0]).ToArray());

                        response.Send(new Result { CorrelationId = envelope.CorrelationId, Text = await result.GetResultAsync() });
                    }
                }
            }
        }

        public void Dispose()
        {
            // clean up, if any
        }
    }
}
