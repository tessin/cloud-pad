using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace lp2azfn
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("missing argument: <LINQPad script>");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(2);
                return;
            }

            if (args[0] == "--install")
            {
                var sendToFolder = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
                var shortcutFileName = Path.Combine(sendToFolder, "Deploy LINQPad script to Azure.lnk");

                if (File.Exists(shortcutFileName))
                {
                    File.Delete(shortcutFileName);
                }

                var sh = new IWshRuntimeLibrary.WshShell();

                var shortcut = (IWshRuntimeLibrary.IWshShortcut)sh.CreateShortcut(shortcutFileName);
                shortcut.Description = "Deploy LINQPad script to Azure";
                shortcut.TargetPath = typeof(Program).Assembly.Location;
                shortcut.Save();
                return;
            }

            if (args[0].StartsWith("-"))
            {
                Console.Error.WriteLine($"unknown option: {args[0]}");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(2);
                return;
            }

            var lpScriptPath = Path.GetFullPath(args[0]);
            if (!File.Exists(lpScriptPath))
            {
                Console.Error.WriteLine($"'{lpScriptPath}' not found");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
                return;
            }

            var q = new LINQPadQuery();
            if (!q.Load(lpScriptPath))
            {
                Console.Error.WriteLine($"'{lpScriptPath}' is not a LINQPad script");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
                return;
            }

            var lpScriptBaseName = Path.GetFileNameWithoutExtension(lpScriptPath); // Azure function name aka LINQPad script base name
            var lpScriptDir = Path.GetDirectoryName(lpScriptPath);

            var funConfigPath = ProbePath($"{lpScriptBaseName}.function.json", new[] { lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
            if (funConfigPath == null)
            {
                var funConfig2 = ProbePath("function.json", new[] { lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                if (funConfig2 == null)
                {
                    Console.Error.WriteLine($"'function.json' not found");
                    if (Environment.UserInteractive)
                    {
                        Console.ReadLine();
                    }
                    Environment.Exit(1);
                    return;
                }
                funConfigPath = funConfig2;
            }

            JObject funConfig;
            try
            {
                funConfig = JObject.Parse(File.ReadAllText(funConfigPath));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"'{funConfigPath}' is not a JSON file. {ex.Message}");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
                return;
            }

            var lpExePath = ProbePath("LINQPad.exe", new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "LINQPad5"), lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
            if (lpExePath == null)
            {
                Console.Error.WriteLine("'LINQPad.exe' not found");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
                return;
            }

            Assembly lpExe;
            try
            {
                lpExe = Assembly.ReflectionOnlyLoadFrom(lpExePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"'{lpExePath}' is not a .NET assembly. {ex.Message}");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
                return;
            }

            var azfnPubSettings = ProbePath("AzureFn.PublishSettings", new[] { lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
            if (azfnPubSettings == null)
            {
                Console.Error.WriteLine("'AzureFn.PublishSettings' not found");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
                return;
            }

            XElement publishData;
            try
            {
                publishData = XElement.Load(azfnPubSettings);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"'{azfnPubSettings}' is not an XML file. {ex.Message}");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
                return;
            }
            var publishProfile = publishData.Element("publishProfile");
            var publishUrl = publishProfile.Attribute("publishUrl").Value;
            var publishUser = publishProfile.Attribute("userName").Value;
            var publishPass = publishProfile.Attribute("userPWD").Value;
            var publishSite = publishProfile.Attribute("msdeploySite").Value;

            foreach (var conn in q.metadata_.Elements("Connection"))
            {
                var password = conn.Element("Password");
                if (password == null)
                {
                    continue;
                }

                string password2;
                try
                {
                    password2 = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(password.Value), lpExe.GetName().GetPublicKey(), DataProtectionScope.CurrentUser));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"'{lpScriptPath}' contains data that cannot be read by this user. It's likely that the file has been copied between different users. {ex.Message}");
                    if (Environment.UserInteractive)
                    {
                        Console.ReadLine();
                    }
                    Environment.Exit(1);
                    return;
                }

                var driverData = conn.Element("DriverData");
                if (driverData == null)
                {
                    conn.Add(driverData = new XElement("DriverData"));
                }

                var extraCxOptions = driverData.Element("ExtraCxOptions");
                if (extraCxOptions == null)
                {
                    driverData.Add(extraCxOptions = new XElement("ExtraCxOptions"));
                }

                var cb = new SqlConnectionStringBuilder(extraCxOptions.Value);
                if (!cb.IntegratedSecurity)
                {
                    cb.Password = password2;
                }

                extraCxOptions.Value = cb.ToString();
            }

            var fileSet = new List<string>();

            foreach (var r in q.metadata_.Elements("Reference"))
            {
                var abs = r.Value;
                if (File.Exists(abs))
                {
                    fileSet.Add(abs);
                    continue;
                }
                var rel = r.Attribute("Relative").Value;
                var path = Path.GetDirectoryName(r.Value);
                var abs2 = ProbePath(rel, new[] { path, lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                if (abs2 == null)
                {
                    Console.Error.WriteLine($"'{lpScriptPath}' assembly reference '{rel}' not found");
                    if (Environment.UserInteractive)
                    {
                        Console.ReadLine();
                    }
                    Environment.Exit(1);
                    return;
                }
                fileSet.Add(abs2);
            }

            // attachments

            var attachmentsPath = ProbePath($"{lpScriptBaseName}.files.txt", new[] { lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
            if (attachmentsPath != null)
            {
                foreach (var line in File.ReadAllLines(attachmentsPath))
                {
                    var line2 = line.Trim();
                    if (string.IsNullOrEmpty(line2))
                    {
                        continue;
                    }
                    var attachmentPath = ProbePath(line2, new[] { lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                    if (attachmentPath == null)
                    {
                        Console.Error.WriteLine($"'{line2}' attachment not found");
                        if (Environment.UserInteractive)
                        {
                            Console.ReadLine();
                        }
                        Environment.Exit(1);
                        return;
                    }
                    fileSet.Add(attachmentPath);
                }
            }

            var attachmentsPath2 = ProbePath("files.txt", new[] { lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
            if (attachmentsPath2 != null)
            {
                foreach (var line in File.ReadAllLines(attachmentsPath))
                {
                    var line2 = line.Trim();
                    if (string.IsNullOrEmpty(line2))
                    {
                        continue;
                    }
                    var attachmentPath = ProbePath(line2, new[] { lpScriptDir, Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory });
                    if (attachmentPath == null)
                    {
                        Console.Error.WriteLine($"'{line2}' global attachment not found");
                        if (Environment.UserInteractive)
                        {
                            Console.ReadLine();
                        }
                        Environment.Exit(1);
                        return;
                    }
                    fileSet.Add(attachmentPath);
                }
            }

            // begin publish

            Console.WriteLine($"Deployment of '{Path.GetFileName(lpScriptPath)}' to '{publishSite}' started");

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://" + publishUrl),
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(publishUser+":"+publishPass))),
                }
            };

            // ensure function folder structure

            using (var res = httpClient.GetAsync($"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/").Result)
            {
                Console.Write('.'); // progress...

                if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    using (var res2 = httpClient.PutAsync($"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/", null).Result)
                    {
                        AssertSuccess(res2);
                    }
                }
            }

            // sync...

            using (var res = httpClient.GetAsync($"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/").Result)
            {
                AssertSuccess(res);

                var arr = JArray.Parse(res.Content.ReadAsStringAsync().Result);
                foreach (var item in arr)
                {
                    var name = (string)item["name"];
                    if (!fileSet.Any(x => Path.GetFileName(x) == name))
                    {
                        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/{Uri.EscapeDataString(name)}");
                        req.Headers.TryAddWithoutValidation("If-Match", "*");
                        using (var res2 = httpClient.SendAsync(req).Result)
                        {
                            AssertSuccess(res2);
                        }
                    }
                }
            }

            foreach (var file in fileSet)
            {
                using (var inputStream = File.OpenRead(file))
                {
                    var req = new HttpRequestMessage(HttpMethod.Put, $"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/{Uri.EscapeDataString(Path.GetFileName(file))}");
                    req.Content = new StreamContent(inputStream);
                    req.Headers.TryAddWithoutValidation("If-Match", "*");
                    using (var res = httpClient.SendAsync(req).Result)
                    {
                        AssertSuccess(res);
                    }
                }
            }

            // deploy LINQPad script
            {
                var outputStream = new MemoryStream();
                q.Save(outputStream);
                outputStream.Position = 0;
                var req = new HttpRequestMessage(HttpMethod.Put, $"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/{Uri.EscapeDataString(Path.GetFileName(lpScriptPath))}");
                req.Content = new StreamContent(outputStream);
                req.Headers.TryAddWithoutValidation("If-Match", "*");
                using (var res = httpClient.SendAsync(req).Result)
                {
                    AssertSuccess(res);
                }
            }


            // deploy function.json
            {
                funConfig["source"] = "run.csx";

                var req = new HttpRequestMessage(HttpMethod.Put, $"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/function.json");
                req.Headers.TryAddWithoutValidation("If-Match", "*");
                req.Content = new StringContent(funConfig.ToString());
                using (var res = httpClient.SendAsync(req).Result)
                {
                    AssertSuccess(res);
                }
            }

            // deploy run.csx
            using (var run = typeof(Program).Assembly.GetManifestResourceStream("lp2azfn.run.csx"))
            {
                var runScript = new StreamReader(run).ReadToEnd();
                runScript = runScript.Replace("<insert azure function name here>", lpScriptBaseName);
                runScript = runScript.Replace("<insert LINQPad script file name here>", Path.GetFileName(lpScriptPath));
                var req = new HttpRequestMessage(HttpMethod.Put, $"/api/vfs/site/wwwroot/{Uri.EscapeDataString(lpScriptBaseName)}/run.csx");
                req.Headers.TryAddWithoutValidation("If-Match", "*");
                req.Content = new StringContent(runScript);
                using (var res = httpClient.SendAsync(req).Result)
                {
                    AssertSuccess(res);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Done");

            System.Threading.Thread.Sleep(1000);
        }

        private static void AssertSuccess(HttpResponseMessage res)
        {
            if (res.IsSuccessStatusCode)
            {
                Console.Write('.'); // OK, progress...
            }
            else
            {
                Console.WriteLine('!'); // Nope
                Console.WriteLine($"Request {res.RequestMessage.Method} {res.RequestMessage.RequestUri.PathAndQuery} failed with response {(int)res.StatusCode} {res.ReasonPhrase}");
                if (Environment.UserInteractive)
                {
                    Console.ReadLine();
                }
                Environment.Exit(1);
            }
        }

        static string ProbePath(string fn, string[] paths)
        {
            foreach (var path in paths)
            {
                var fn2 = Path.Combine(path, fn);
                if (File.Exists(fn2))
                {
                    return fn2;
                }
            }
            return null;
        }
    }
}
