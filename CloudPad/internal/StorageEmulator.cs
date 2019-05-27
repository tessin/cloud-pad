using System.Diagnostics;
using System.IO;
using System.Net;

namespace CloudPad.Internal
{
    static class StorageEmulator
    {
        public static void StartOrInstall()
        {
            if (!HasProcess("AzureStorageEmulator"))
            {
                if (Start())
                {
                    // ok, done
                }
                else
                {
                    var text = "The Azure Storage Emulator does not appear to be installed. You will need the Azure Storage Emulator for the Azure Functions Host to work. Would you like to install it now?";
                    var caption = "Azure Storage Emulator";

                    if (MessageBox.ShowYesNoQuestion(text, caption))
                    {
                        using (var tempFile = new TempFile(".msi"))
                        {
                            var req = WebRequest.Create($"https://go.microsoft.com/fwlink/?linkid=717179");
                            using (var res = req.GetResponse())
                            {
                                using (var zip = File.Create(tempFile.FileName))
                                {
                                    res.GetResponseStream().CopyTo(zip);
                                }

                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = @"C:\Windows\System32\msiexec.exe",
                                    Arguments = $"/i \"{tempFile.FileName}\"",
                                    UseShellExecute = true,
                                    Verb = "runas"
                                };

                                using (var p = Process.Start(startInfo))
                                {
                                    p.WaitForExit();

                                    if (p.ExitCode == 0)
                                    {
                                        if (!Start())
                                        {
                                            Trace.WriteLine("Cannot start storage emulator.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool Start()
        {
            var azureStorageEmulatorPath = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe";
            if (File.Exists(azureStorageEmulatorPath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = azureStorageEmulatorPath,
                    Arguments = "start",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var p = Process.Start(startInfo))
                {
                    p.WaitForExit();
                }

                return true;
            }
            return false;
        }

        private static bool HasProcess(string processName)
        {
            var hasProcess = false;
            foreach (var p in Process.GetProcessesByName(processName))
            {
                hasProcess = true;
                p.Dispose();
            };
            return hasProcess;
        }
    }
}
