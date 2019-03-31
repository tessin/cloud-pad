
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Tessin.Internal;

namespace CloudPad
{
  public class FileDependency
  {
    internal static Dictionary<string, string> fileDependencies = new Dictionary<string, string>();

    static FileDependency()
    {
      var vfsRoot = Path.Combine(Path.GetDirectoryName(Internal.LINQPad.GetCurrentQueryPath()), "vfs");
      var indexFileName = Path.Combine(vfsRoot, "index.json");
      if (File.Exists(indexFileName))
      {
        foreach (var entry in JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(indexFileName)))
        {
          fileDependencies[entry.Key] = Path.Combine(vfsRoot, entry.Value);
        }
      }
    }

    private readonly string fileName;

    public FileDependency(string fileName)
    {
      // if build context, resolve file and embed in build context

      string abs;

      if (Path.IsPathRooted(fileName))
      {
        abs = fileName;
      }
      else
      {
        abs = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Internal.LINQPad.GetCurrentQueryPath()), fileName));
      }

      if (Env.IsCompiling)
      {
        if (!File.Exists(abs))
        {
          throw new FileNotFoundException($"cannot resolve file '{fileName}'", abs);
        }
      }

      if (!fileDependencies.ContainsKey(fileName))
      {
        fileDependencies[fileName] = abs;
      }

      this.fileName = fileName;
    }

    public Stream OpenRead()
    {
      return File.OpenRead(fileDependencies[fileName]);
    }
  }
}