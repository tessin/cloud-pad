using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace CloudPad.Internal {
  class KuduClient {
    private Uri _baseUrl;
    public string Host => _baseUrl.Host;
    private string _auth;

    public KuduClient(Uri baseUrl, string auth) {
      this._baseUrl = baseUrl;
      this._auth = auth;
    }

    public static KuduClient FromPublishProfile(string publishSettingsFileName) {
      var publishSettings = XElement.Load(publishSettingsFileName);
      var publishProfile = publishSettings.Elements("publishProfile").Where(el => (string)el.Attribute("publishMethod") == "MSDeploy").Single();

      var publishUrl = new Uri("https://" + (string)publishProfile.Attribute("publishUrl"));
      var userName = (string)publishProfile.Attribute("userName");
      var userPWD = (string)publishProfile.Attribute("userPWD");
      var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + userPWD));

      return new KuduClient(publishUrl, auth);
    }

    public HttpWebRequest CreateRequest(string method, string requestUrl) {
      var req = WebRequest.CreateHttp(new Uri(_baseUrl, requestUrl));
      req.Method = method;
      req.Headers.Set(HttpRequestHeader.Authorization, _auth);
      return req;
    }

    public HttpWebResponse Do(HttpWebRequest req) {
      try {
        return (HttpWebResponse)req.GetResponse();
      } catch (WebException we) {
        var resp = we.Response as HttpWebResponse;
        if (resp == null)
          throw;
        return resp;
      }
    }

    public void Check(HttpWebResponse res) {
      if (!(200 <= (int)res.StatusCode && (int)res.StatusCode <= 299)) {
        throw new InvalidOperationException(new StreamReader(res.GetResponseStream()).ReadToEnd());
      }
    }

    public void ZipDeploy(string dir) {
      using (var tempFile = new TempFile()) {
        ZipFile.CreateFromDirectory(dir, tempFile.FileName);

        var req = CreateRequest("POST", "/api/zipdeploy");
        using (var outStream = req.GetRequestStream()) {
          using (var inStream = File.OpenRead(tempFile.FileName)) {
            inStream.CopyTo(outStream);
          }
        }
        using (var res = Do(req)) {
          Check(res);
        }
      }
    }

    public void ZipUpload(string dir, string path = @"site/wwwroot") {
      using (var tempFile = new TempFile()) {
        ZipFile.CreateFromDirectory(dir, tempFile.FileName);

        var req = CreateRequest("PUT", "/api/zip/" + path);
        using (var outStream = req.GetRequestStream()) {
          using (var inStream = File.OpenRead(tempFile.FileName)) {
            inStream.CopyTo(outStream);
          }
        }
        using (var res = Do(req)) {
          Check(res);
        }
      }
    }
  }
}
