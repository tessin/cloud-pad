using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace CloudPad.Internal
{
    public class KuduClient
    {
        private Uri _baseUrl;
        public string Host => _baseUrl.Host;
        private string _auth;

        public KuduClient(Uri baseUrl, string auth)
        {
            this._baseUrl = baseUrl;
            this._auth = auth;
        }

        public static KuduClient FromPublishProfile(string publishSettingsFileName)
        {
            var publishSettings = XElement.Load(publishSettingsFileName);
            var publishProfile = publishSettings.Elements("publishProfile").Where(el => (string)el.Attribute("publishMethod") == "MSDeploy").Single();

            var publishUrl = new Uri("https://" + (string)publishProfile.Attribute("publishUrl"));
            var userName = (string)publishProfile.Attribute("userName");
            var userPWD = (string)publishProfile.Attribute("userPWD");
            var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + userPWD));

            return new KuduClient(publishUrl, auth);
        }

        public HttpWebRequest CreateRequest(string method, string requestUrl)
        {
            var req = WebRequest.CreateHttp(new Uri(_baseUrl, requestUrl));
            req.Method = method;
            req.Headers.Set(HttpRequestHeader.Authorization, _auth);
            return req;
        }

        public HttpWebResponse Do(HttpWebRequest req)
        {
            try
            {
                return (HttpWebResponse)req.GetResponse();
            }
            catch (WebException we)
            {
                var resp = we.Response as HttpWebResponse;
                if (resp == null)
                    throw;
                return resp;
            }
        }

        public HttpWebResponse DoFile(HttpWebRequest req, string path)
        {
            using (var outStream = req.GetRequestStream())
            {
                using (var inStream = File.OpenRead(path))
                {
                    inStream.CopyTo(outStream);
                }
            }
            return Do(req);
        }

        public HttpWebResponse DoJson(HttpWebRequest req, object payload)
        {
            req.ContentType = "application/json";
            using (var reqStream = req.GetRequestStream())
            {
                var text = JsonConvert.SerializeObject(payload);
                var bytes = Encoding.UTF8.GetBytes(text);
                reqStream.Write(bytes, 0, bytes.Length);
            }
            return Do(req);
        }

        public void Check(HttpWebResponse res)
        {
            if (!(200 <= (int)res.StatusCode && (int)res.StatusCode <= 299))
            {
                throw new InvalidOperationException(new StreamReader(res.GetResponseStream()).ReadToEnd());
            }
        }

        public T CheckJson<T>(HttpWebResponse res)
        {
            Check(res);

            using (var reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8, false))
            {
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }

        // ================================

        public void ZipDeploy(string dir)
        {
            using (var tempFile = new TempFile())
            {
                ZipFile.CreateFromDirectory(dir, tempFile.FileName);

                var req = CreateRequest("POST", "/api/zipdeploy");
                using (var res = DoFile(req, tempFile.FileName))
                {
                    Check(res);
                }
            }
        }

        public void ZipDeployPackage(Uri packageUri)
        {
            var req = CreateRequest("PUT", "/api/zipdeploy");
            using (var res = DoJson(req, new { packageUri }))
            {
                Check(res);
            }
        }

        public void ZipUpload(string dir, string path = @"site/wwwroot")
        {
            using (var tempFile = new TempFile())
            {
                ZipFile.CreateFromDirectory(dir, tempFile.FileName);

                var req = CreateRequest("PUT", "/api/zip/" + path);
                using (var res = DoFile(req, tempFile.FileName))
                {
                    Check(res);
                }
            }
        }

        // ================================

        public bool VfsExists(string path)
        {
            var req = CreateRequest("HEAD", "/api/vfs/" + path);
            using (var res = Do(req))
            {
                return res.StatusCode == HttpStatusCode.OK;
            }
        }

        // ================================

        public Dictionary<string, string> GetSettings()
        {
            var req = CreateRequest("GET", "/api/settings");
            using (var res = Do(req))
            {
                return CheckJson<Dictionary<string, string>>(res);
            }
        }

        /// <summary>
        /// Note, this API changes Kudu/AzureWebJobs settings. Cannot be used to set application settings.
        /// </summary>
        public void PostSettings(Dictionary<string, string> settings)
        {
            var req = CreateRequest("POST", "/api/settings");
            using (var res = DoJson(req, settings))
            {
                Check(res);
            }
        }
    }
}
