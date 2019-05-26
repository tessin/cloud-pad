using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad {
  static class FirstRun {
    public static string Lockfile => Path.Combine(Env.GetLocalAppDataDirectory(), "first_run");

    public static bool ShouldPrompt() {
      if (Environment.UserInteractive) {
        if (!File.Exists(Lockfile)) {
          return true;
        }
      }
      return false;
    }

    public static void Prompt() {
      var mbType = Type.GetType("System.Windows.Forms.MessageBox, System.Windows.Forms");

      var show = mbType.GetMethod("Show", BindingFlags.Public | BindingFlags.Static, null, new[] {
        typeof(string),
        typeof(string),
        Type.GetType("System.Windows.Forms.MessageBoxButtons, System.Windows.Forms"),
        Type.GetType("System.Windows.Forms.MessageBoxIcon, System.Windows.Forms"),
      }, null);

      var result = show.Invoke(null, new object[] {
        "Looks like this is your first run. Would you like to add a Explorer context menu item to help with deployment of scripts to Azure?",
        "Welcome to CloudPad!",
        4, // YesNo
        0x20 // Question
      });

      if (Convert.ToInt32(result) == 6) {  // Yes
        var startInfo = new ProcessStartInfo();

        startInfo.FileName = @"C:\Program Files (x86)\LINQPad5\LPRun.exe";
        startInfo.Arguments = $"\"{Util.CurrentQueryPath}\" -install";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";
#if !DEBUG
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif

        using (var p = Process.Start(startInfo)) {
          p.WaitForExit();
        }
      }

      File.WriteAllText(Lockfile, "");
    }

    public static void Install() {
      using (var shell = Registry.ClassesRoot.OpenSubKey(@"LINQPad\shell", true)) {
        using (var publish = shell.CreateSubKey("publish", true)) {
          publish.SetValue("", "Publish LINQPad script to Azure");
          publish.SetValue("Icon", @"C:\\Program Files (x86)\\LINQPad5\\LINQPad.EXE,0");
          using (var command = publish.CreateSubKey("command", true)) {
            command.SetValue("", "\"C:\\Program Files (x86)\\LINQPad5\\LPRun.EXE\" \"%1\" -publish -interactive");
          }
        }
      }
    }
  }
}
