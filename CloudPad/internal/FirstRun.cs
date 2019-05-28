using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace CloudPad.Internal
{
    static class FirstRun
    {
        public static string Lockfile => Path.Combine(Env.GetLocalAppDataDirectory(), "first_run");

        public static bool ShouldPrompt()
        {
            if (Environment.UserInteractive)
            {
                if (!File.Exists(Lockfile))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Prompt()
        {
            var text = "Looks like this is your first run. Would you like to add a Explorer context menu item to help with deployment of scripts to Azure?";
            var caption = "Welcome to CloudPad!";

            if (MessageBox.ShowYesNoQuestion(text, caption))
            {
                var startInfo = new ProcessStartInfo();

                startInfo.FileName = @"C:\Program Files (x86)\LINQPad5\LPRun.exe";
                startInfo.Arguments = $"\"{Util.CurrentQueryPath}\" -install";
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
#if !DEBUG
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif

                using (var p = Process.Start(startInfo))
                {
                    p.WaitForExit();
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Lockfile));
            File.WriteAllText(Lockfile, "");
        }

        public static void Install()
        {
            using (var shell = Registry.ClassesRoot.OpenSubKey(@"LINQPad\shell", true))
            {
                using (var publish = shell.CreateSubKey("publish", true))
                {
                    publish.SetValue("", "Publish LINQPad script to Azure");
                    publish.SetValue("Icon", @"C:\\Program Files (x86)\\LINQPad5\\LINQPad.EXE,0");
                    using (var command = publish.CreateSubKey("command", true))
                    {
                        command.SetValue("", "\"C:\\Program Files (x86)\\LINQPad5\\LPRun.EXE\" \"%1\" -publish -interactive");
                    }
                }
            }
        }
    }
}
