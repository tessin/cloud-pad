using System;
using System.IO;
using System.IO.Pipes;

namespace CloudPad.Internal
{
    public class DuplexPipe : IDisposable
    {
        public PipeStream OutPipe { get; }
        public PipeStream InPipe { get; }

        /// <summary>
        /// Create a pipe server.
        /// </summary>
        public DuplexPipe()
        {
            OutPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            InPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        }

        /// <summary>
        /// Create a pipe client.
        /// </summary>
        public DuplexPipe(string inPipeClientHandle, string outPipeClientHandle)
        {
            OutPipe = new AnonymousPipeClientStream(PipeDirection.Out, outPipeClientHandle);
            InPipe = new AnonymousPipeClientStream(PipeDirection.In, inPipeClientHandle);
        }

        public string GetClientHandleAsString()
        {
            return ((AnonymousPipeServerStream)OutPipe).GetClientHandleAsString() + " " + ((AnonymousPipeServerStream)InPipe).GetClientHandleAsString();
        }

        public void DisposeLocalCopyOfClientHandle()
        {
            ((AnonymousPipeServerStream)OutPipe).DisposeLocalCopyOfClientHandle();
            ((AnonymousPipeServerStream)InPipe).DisposeLocalCopyOfClientHandle();
        }

        public void Dispose()
        {
            OutPipe.Dispose();
            InPipe.Dispose();
        }
    }
}
