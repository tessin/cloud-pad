using System;
using System.IO;
using System.Diagnostics;

public static void Run(TimerInfo timerTrigger, TraceWriter log)
{
  log.Info("LINQPad script triggered");

  var home = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process);

  var startInfo = new ProcessStartInfo {
    WorkingDirectory = Path.Combine(home, @"site\wwwroot\<insert azure function name here>"),
    FileName = Path.Combine(home, @"data\LINQPad5-AnyCPU\lprun.exe"),
    Arguments = "<insert LINQPad script file name here>",
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
  };

  using (var p = new Process()) {
    p.StartInfo = startInfo;

    p.OutputDataReceived += (sender, e) => {
      log.Info(e.Data);
    };

    p.ErrorDataReceived += (sender, e) => {
      log.Error(e.Data);
    };

    p.EnableRaisingEvents = true;

    p.Start();

    p.BeginOutputReadLine();
    p.BeginErrorReadLine();

    p.WaitForExit();

    if (p.ExitCode != 0) {
      log.Error($"LINQPad script exited with non-zero exit code: {p.ExitCode}");
      return;
    }
  }

  log.Info($"LINQPad script completed successfully");
  return;
}