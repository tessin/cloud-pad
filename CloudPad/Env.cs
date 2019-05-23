using System;
using System.IO;

namespace CloudPad {
  static class Env {
    public static string GetLocalAppDataDirectory() {
      return Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "CloudPad");
    }

    public static string GetProgramDataDirectory() {
      return Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "CloudPad");
    }
  }
}
