using Microsoft.Azure.WebJobs.Host;
using System.Diagnostics;

namespace CloudPadFunctionHost
{
    public class TraceWriterListener : TraceListener
    {
        private readonly TraceWriter _log;

        private TraceWriterListener()
        {
            Trace.Listeners.Add(this);
            Debug.Listeners.Add(this);
        }

        public TraceWriterListener(TraceWriter log)
            : this()
        {
            _log = log;
        }

        public override void Write(string message)
        {
            _log.Verbose(message);
        }

        public override void WriteLine(string message)
        {
            _log.Verbose(message);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Trace.Listeners.Remove(this);
                Debug.Listeners.Remove(this);
            }
        }
    }
}
