using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloudPad.Internal {
  class FileUtil {
    public static IEnumerable<string> ResolveSearchPatternUpDirectoryTree(string dir, string searchPattern) {
      while (dir != null) {
        var q = Directory.EnumerateFiles(dir, searchPattern, SearchOption.TopDirectoryOnly);
        if (q.Any()) {
          return q;
        }
        dir = Path.GetDirectoryName(dir);
      }
      return Enumerable.Empty<string>();
    }
  }
}
