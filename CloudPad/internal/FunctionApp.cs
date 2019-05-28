using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace CloudPad.Internal
{
    static class FunctionApp
    {
        public static readonly Uri PackageUri = new Uri("https://github.com/tessin/cloud-pad/releases/download/2.0.0/CloudPad.FunctionApp.zip");

        public static void Deploy(string workingDirectory)
        {
            DeployDebug(workingDirectory);

            if (!File.Exists(Path.Combine(workingDirectory, "bin", "CloudPad.FunctionApp.dll")))
            {
                // Deploy the CloudPad.FunctionApp runtime...
                using (var tempFile = new TempFile())
                {
                    var req = WebRequest.Create(PackageUri);
                    using (var res = req.GetResponse())
                    {
                        using (var zip = File.Create(tempFile.FileName))
                        {
                            res.GetResponseStream().CopyTo(zip);
                        }
                    }
                    ZipFile.ExtractToDirectory(tempFile.FileName, workingDirectory);
                }
            }
        }

        [Conditional("DEBUG")]
        private static void DeployDebug(string workingDirectory)
        {
            var source = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Source\tessin\cloud-pad\CloudPad.FunctionApp\bin\Debug\net461");

            var sourceHost = Path.Combine(source, "host.json");
            var sourceBin = Path.Combine(source, "bin") + "\\";  // this trailing slash is important!

            CopyIfNeeded(sourceHost, Path.Combine(workingDirectory, Path.GetFileName(sourceHost)));

            foreach (var src in Directory.EnumerateFiles(sourceBin, "*", SearchOption.AllDirectories))
            {
                var rel = src.Substring(sourceBin.Length);
                var dst = Path.Combine(workingDirectory, "bin", rel);
                CopyIfNeeded(src, dst);
            }
        }

        private static void CopyIfNeeded(string src, string dst)
        {
            if (File.Exists(dst))
            {
                var srcLastWriteTimeUtc = File.GetLastWriteTimeUtc(src);
                var dstLastWriteTimeUtc = File.GetLastWriteTimeUtc(dst);
                if (!(dstLastWriteTimeUtc < srcLastWriteTimeUtc))
                {
                    return; // source is not same (i.e. not more recent)
                }
            }
            Debug.WriteLine($"Copy '{src}' -> '{dst}'");
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, true);
        }
    }
}
