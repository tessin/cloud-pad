using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    class AzResult
    {
        public int ExitCode { get; set; }
        public bool Success => ExitCode == 0;
        public bool HasError => ExitCode != 0;
        public JToken Output { get; set; }
        public string Error { get; set; }
    }

    static class Az
    {
        public static Task<AzResult> RunAsunc(params string[] args)
        {
            return RunAsunc((IEnumerable<string>)args);
        }

        public static async Task<AzResult> RunAsunc(IEnumerable<string> args)
        {
            string Escape(string arg)
            {
                if (arg.IndexOfAny(new char[] { '\t', ' ' }) != -1)
                {
                    return "\"" + arg + "\"";
                }
                return arg;
            }

            var command = "az " + string.Join(" ", args.Select(Escape)); // trailing backslashes are problematic

            Debug.WriteLine(command, "Az");

            var startInfo = new ProcessStartInfo
            {
                FileName = @"C:\WINDOWS\system32\cmd.exe",
                Arguments = "/c " + command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            var tcs = new TaskCompletionSource<string>();

            using (var p = Process.Start(startInfo))
            {
                // see https://stackoverflow.com/a/139604/58961

                var stderr = new StringBuilder();
                p.BeginErrorReadLine();
                p.ErrorDataReceived += (sender, e) => stderr.AppendLine(e.Data);

                // do not block main thread, due to the way `WaitForExit` is implemented 
                // it can deadlock if we do when certain synchronization contexts are used
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var stdout = p.StandardOutput.ReadToEnd();
                    Debug.WriteLine("WaitForExit", "Az");
                    p.WaitForExit();
                    Debug.WriteLine("Exited", "Az");
                    tcs.SetResult(stdout);
                });

                var result = await tcs.Task;

                Debug.WriteLine("ExitCode " + p.ExitCode, "Az");

                return new AzResult
                {
                    ExitCode = p.ExitCode,
                    Output = !string.IsNullOrEmpty(result) ? JToken.Parse(result) : null,
                    Error = stderr.ToString(),
                };
            }
        }
    }
}
