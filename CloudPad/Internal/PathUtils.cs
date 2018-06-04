using System;
using System.IO;
using System.Linq;

namespace CloudPad.Internal
{
    class PathUtils
    {
        public static string Resolve(string searchPattern)
        {
            var currentDirectory = Environment.CurrentDirectory;
            while (currentDirectory != null)
            {
                var file = Directory.EnumerateFiles(currentDirectory, searchPattern).SingleOrDefault();
                if (file != null)
                {
                    return file;
                }
                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }
            return null;
        }
    }
}
