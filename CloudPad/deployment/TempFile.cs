using System;
using System.IO;

namespace CloudPad.Internal
{
    public class TempFile : IDisposable
    {
        public string FileName { get; }

        public TempFile(string extension = null)
        {
            FileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) + extension;
        }

        public void Dispose()
        {
            File.Delete(FileName);
        }
    }
}
