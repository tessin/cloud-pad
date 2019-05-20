using System;

namespace CloudPad {
  public interface ILogger {
    void Error(string message, Exception ex = null, string source = null);
    void Info(string message, string source = null);
    void Verbose(string message, string source = null);
    void Warning(string message, string source = null);
  }
}
