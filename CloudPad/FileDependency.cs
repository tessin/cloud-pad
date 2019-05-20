using CloudPad.Internal;
using System;
using System.IO;

namespace CloudPad {
  public class FileDependency {
    private readonly string _path;
    private string FileName { get; }

    public FileDependency(string fileName) {
      // note that this code is running under a loader lock like manner
      // it is never called concurrently thus it is safe to call non thread
      // safe code from this code

      if (LoaderLock.IsHeld()) {
        // if the loader lock is held then we are constructing the user query

        var vfs = VirtualFileSystemRoot.GetRoot();

        this._path = vfs.GetFileDependency(fileName);
        this.FileName = fileName;
      } else {
        string abs;

        if (Path.IsPathRooted(fileName)) {
          abs = fileName;
        } else {
          var currentQueryPath = Util.CurrentQueryPath;
          if (currentQueryPath == null) {
            throw new InvalidOperationException("If you use a file dependency with a relative path the LINQPad script must be saved to disk.");
          }
          abs = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(currentQueryPath), fileName));
        }

        if (!File.Exists(abs)) {
          throw new FileNotFoundException($"cannot resolve file '{fileName}'", abs);
        }

        var vfs = VirtualFileSystemRoot.GetRoot();

        vfs.SetFileDependency(fileName, abs);

        this._path = abs;
        this.FileName = fileName;
      }
    }

    public Stream OpenRead() {
      return File.OpenRead(_path);
    }
  }
}
