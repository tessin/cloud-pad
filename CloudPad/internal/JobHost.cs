using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CloudPad.Internal {
  class JobHost {
    private static string GetInstanceId(string currentQueryPath) {
      var hasher = new SHA256Managed();
      var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(currentQueryPath.ToLowerInvariant()));
      return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public static JobHost Launch(string currentQueryPath, string functionAppDirectory = null) {

      var root = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "CloudPad");

      Directory.CreateDirectory(root);

      // ================

      var funcVersion = "1.0.19";
      var funcDir = Path.Combine(root, $"func.{funcVersion}");
      var funcFileName = Path.Combine(funcDir, "func.exe");

      if (!File.Exists(funcFileName)) {
        var azureFunctionsCliZip = funcDir + ".zip";
        var req = WebRequest.Create($"https://functionscdn.azureedge.net/public/{funcVersion}/Azure.Functions.Cli.zip");
        using (var res = req.GetResponse()) {
          using (var zip = File.Create(azureFunctionsCliZip)) {
            res.GetResponseStream().CopyTo(zip);
          }
        }
        ZipFile.ExtractToDirectory(azureFunctionsCliZip, funcDir);
        File.Delete(azureFunctionsCliZip);
      }

      // ================

      var instanceId = GetInstanceId(currentQueryPath);

      var workingDirectory = Path.Combine(root, instanceId);

      functionAppDirectory = functionAppDirectory ?? workingDirectory;

      var funcJsonFileName = workingDirectory + ".json";
      var funcJson = File.Exists(funcJsonFileName) ? JObject.Parse(File.ReadAllText(funcJsonFileName)) : new JObject();

      var pid = (int?)funcJson["pid"];
      if (pid.HasValue && IsJobHost(pid.Value)) {

        // todo: check version compatability

        return new JobHost(pid.Value, functionAppDirectory);
      }

      // todo: if the `functionAppDirectory` does not exist or is empty we need to install the CloudPad.FunctionApp in it

      var funcStartInfo = new ProcessStartInfo {
        FileName = funcFileName,
        Arguments = "host start",
        WorkingDirectory = functionAppDirectory,
        UseShellExecute = false,
      };

      using (var func = Process.Start(funcStartInfo)) {
        var pid2 = func.Id;

        funcJson["pid"] = pid2;

        Directory.CreateDirectory(workingDirectory);
        File.WriteAllText(funcJsonFileName, JsonConvert.SerializeObject(funcJson, Formatting.Indented));

        return new JobHost(pid2, functionAppDirectory);
      }
    }

    private static bool IsJobHost(int pid) {
      try {
        using (var func = Process.GetProcessById(pid)) {
          return "func.exe".Equals(func.MainModule.ModuleName, StringComparison.OrdinalIgnoreCase);
        }
      } catch {
        return false;
      }
    }

    public int Id { get; }
    public string WorkingDirectory { get; }

    public JobHost(int id, string workingDirectory) {
      Id = id;
      WorkingDirectory = workingDirectory;
    }
  }
}
