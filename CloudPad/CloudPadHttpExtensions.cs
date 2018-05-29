using System.Net;
using System.Net.Http;
using System.Text;

namespace CloudPad
{
    public static class CloudPadHttpExtensions
    {
        public static HttpResponseMessage CreateText(this HttpRequestMessage req, string text)
        {
            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Content = new StringContent(text, Encoding.UTF8, "text/plain");
            return res;
        }
    }
}
