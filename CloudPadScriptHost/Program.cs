using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace CloudPadScriptHost
{
    class Run
    {
        public string LINQPadScriptFileName { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (0 < args.Length)
            {
                Debugger.Launch();

                var requestChannel = new AnonymousPipeClientStream(PipeDirection.In, args[0]);
                var responseChannel = new AnonymousPipeClientStream(PipeDirection.In, args[1]);

                ClientAsync(requestChannel, responseChannel).GetAwaiter().GetResult();
            }
            else
            {
                var fn = Environment.GetCommandLineArgs()[0];

                var requestChannel = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                var responseChannel = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

                var processStartInfo = new ProcessStartInfo { UseShellExecute = false };

                processStartInfo.FileName = fn;
                processStartInfo.Arguments = requestChannel.GetClientHandleAsString() + " " + responseChannel.GetClientHandleAsString();

                var child = Process.Start(processStartInfo);

                requestChannel.DisposeLocalCopyOfClientHandle();
                responseChannel.DisposeLocalCopyOfClientHandle();

                Send(new Run { LINQPadScriptFileName = @"C:\Users\leidegre\Source\tessin\CloudPad\lprun.linq" }, requestChannel);
            }

            //var sw = new Stopwatch();

            //sw.Start();

            //var queryCompilation = LINQPad.Util.CompileAsync(@"C:\Users\leidegre\Source\tessin\CloudPad\lprun.linq").GetAwaiter().GetResult();

            //Console.WriteLine(sw.Elapsed);

            //for (int i = 0; i < 3; i++)
            //{
            //    sw.Restart();
            //    var executor = queryCompilation.Run(LINQPad.QueryResultFormat.Text);
            //    executor.WaitAsync().GetAwaiter().GetResult();
            //    Console.WriteLine(sw.Elapsed);
            //}
        }

        private static void Send<T>(T command, PipeStream channel)
        {
            var buffer = new MemoryStream();

            var w = new BinaryWriter(buffer);

            w.Write(0); // reserve header size

            w.Write((byte)1);

            var json = JsonConvert.SerializeObject(command, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });

            w.Write(json);

            if (buffer.TryGetBuffer(out var slice))
            {
                // fix header
                buffer.Position = 0;
                w.Write(slice.Count - 4); // message size - header size

                channel.Write(slice.Array, slice.Offset, slice.Count);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private struct Message
        {
            public int CommandId { get; set; }
            public object Payload { get; set; }
        }

        private static async Task<Message> ReceiveAsync(PipeStream channel)
        {
            var bytes = new byte[4];

            if (await channel.ReadAsync(bytes, 0, 4) < 4)
            {
                return default(Message);
            }

            var size = BitConverter.ToInt32(bytes, 0);

            var bytes2 = new byte[size];

            if (await channel.ReadAsync(bytes2, 0, size) < size)
            {
                throw new InvalidOperationException();
            }

            var buffer = new MemoryStream(bytes2);

            var b = buffer.ReadByte();

            switch (b)
            {
                case 1:
                    var r = new BinaryReader(buffer);
                    var json = r.ReadString();
                    return new Message { CommandId = b, Payload = JsonConvert.DeserializeObject(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects }) };

                default:
                    throw new InvalidOperationException();
            }
        }

        private static async Task ClientAsync(AnonymousPipeClientStream request, AnonymousPipeClientStream response)
        {
            for (; ; )
            {
                var msg = await ReceiveAsync(request);

                switch (msg.CommandId)
                {
                    case 0:
                        return;

                    case 1:
                        break;
                }
            }
        }
    }
}
