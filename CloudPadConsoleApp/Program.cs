using CloudPad;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;

namespace CloudPadApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new CloudPadJobHost(new Program(), args);

            host.WaitAsync().GetAwaiter().GetResult();
        }

        [Route("hello")]
        HttpResponseMessage Hello(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            return req.CreateText("world");
        }
    }
}
