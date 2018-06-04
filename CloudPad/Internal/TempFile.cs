using System;
using System.IO;

namespace CloudPad.Internal
{
    // if you want to keep the file, move the file from the temp location to some other place
    // or just rename the file to some other temporary name, File.Delete does not throw error
    // if file does not exist

    class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile(string ext = "tmp")
        {
            _Next:
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + "." + ext);
            if (File.Exists(path))
            {
                goto _Next;
            }
            this.Path = path;
        }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
