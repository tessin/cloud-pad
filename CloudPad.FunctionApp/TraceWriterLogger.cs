using Microsoft.Azure.WebJobs.Host;
using System;

namespace CloudPad.FunctionApp {
  class TraceWriterLogger : ILogger {
    private readonly TraceWriter _log;

    public TraceWriterLogger(TraceWriter log) {
      this._log = log;
    }

    public void Error(string message, Exception ex = null, string source = null) {
      _log.Error(message, ex, source);
    }

    public void Info(string message, string source = null) {
      _log.Info(message, source);
    }

    public void Verbose(string message, string source = null) {
      _log.Verbose(message, source);
    }

    public void Warning(string message, string source = null) {
      _log.Warning(message, source);
    }
  }
}
