using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace CloudPad.Internal {
  class VirtualFileSystemRoot {
    private static readonly Dictionary<string, VirtualFileSystemRoot> _vfs = new Dictionary<string, VirtualFileSystemRoot>();

    public static VirtualFileSystemRoot GetRoot() {
      var applicationBase = LoaderLock.ApplicationBase ?? string.Empty;
      if (_vfs.TryGetValue(applicationBase, out var root)) {
        return root;
      }
      var newRoot = new VirtualFileSystemRoot();
      if (0 < applicationBase.Length) {
        newRoot.LoadFrom(applicationBase);
      }
      _vfs.Add(applicationBase, newRoot);
      return newRoot;
    }

    private Dictionary<string, string> _fileDependencies = new Dictionary<string, string>();

    public void SaveTo(string applicationBase) {
      var index = new Dictionary<string, string>();
      var vfs = Path.Combine(applicationBase, "vfs");
      Directory.CreateDirectory(vfs);
      foreach (var fd in _fileDependencies) {
        string hash;
        using (var inp = File.OpenRead(fd.Value)) {
          var sha256 = new SHA256Managed();
          hash = BitConverter.ToString(sha256.ComputeHash(inp)).Replace("-", "");
        }
        var path = Path.Combine(vfs, hash);
        if (!File.Exists(path)) {
          File.Copy(fd.Value, path);
        }
        index[fd.Key] = hash;
      }

      File.WriteAllText(Path.Combine(vfs, "index.json"), JsonConvert.SerializeObject(index, Formatting.Indented));
    }

    public void LoadFrom(string applicationBase) {
      var vfs = Path.Combine(applicationBase, "vfs");
      var index = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(vfs, "index.json")));
      foreach (var item in index) {
        _fileDependencies[item.Key] = Path.Combine(vfs, item.Value);
      }
    }

    public void SetFileDependency(string fileName, string path) {
      _fileDependencies[fileName] = path;
    }

    public string GetFileDependency(string fileName) {
      return _fileDependencies[fileName];
    }
  }
}
