
using System.Collections.Generic;
using System.IO;
using Tessin.Internal;

namespace CloudPad
{
  public class FileDependency
  {
    internal static Dictionary<string, string> fileDependencies = new Dictionary<string, string>();

    private readonly string fileName;

    public FileDependency(string fileName)
    {
      // if build context, resolve file and embed in build context

      var abs = fileName;

      if (Path.IsPathRooted(abs))
      {
        // ok, absolute
      }
      else
      {
        // resolve relative file
        abs = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Internal.LINQPad.GetCurrentQueryPath()), fileName));
      }

      if (Env.IsCompiling)
      {
        if (!File.Exists(abs))
        {
          throw new FileNotFoundException($"cannot resolve file '{fileName}'", abs);
        }
        fileDependencies[fileName] = abs;
      }

      this.fileName = fileName;
    }

    public Stream OpenRead()
    {
      return null; // open file for reading 
    }
  }
}