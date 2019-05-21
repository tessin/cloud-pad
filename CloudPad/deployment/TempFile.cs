using System;
using System.IO;

namespace CloudPad.Internal {
  class TempFile : IDisposable {
    public string FileName { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose() {
      File.Delete(FileName);
    }
  }
}
