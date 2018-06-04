using System;
using System.IO;
using System.Text;

namespace CloudPad.Internal
{
    class Shortcut
    {
        public static void Install()
        {
            var sendToFolder = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
            var shortcutFileName = Path.Combine(sendToFolder, "Deploy LINQPad script to Azure.lnk");

            if (File.Exists(shortcutFileName))
            {
                File.Delete(shortcutFileName);
            }

            var fn = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\LINQPadAzureFunction.cmd");

            var script = new[] {
                "@echo off",
                "cd %~dp1",
                @"""C:\Program Files (x86)\LINQPad5\LPRun.exe"" %1 -publish *.PublishSettings",
            };

            File.WriteAllLines(fn, script, Encoding.ASCII);

            var sh = new IWshRuntimeLibrary.WshShell();

            var shortcut = (IWshRuntimeLibrary.IWshShortcut)sh.CreateShortcut(shortcutFileName);
            shortcut.Description = "Deploy LINQPad script to Azure";
            shortcut.TargetPath = fn;
            shortcut.IconLocation = @"C:\Program Files (x86)\LINQPad5\LINQPad.exe";
            shortcut.Save();
        }
    }
}
