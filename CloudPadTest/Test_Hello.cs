using CloudPad.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudPad
{
    [TestClass]
    public class Test_Hello
    {
        [TestMethod]
        public async Task Test_Hello_Test()
        {
            Console.WriteLine(Environment.CurrentDirectory);

            var linqPadScriptFileName = Path.GetFullPath(@"..\..\..\test_hello.linq");

            using (var invoker = new Invoker())
            {
                // sync request handler
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/api/test"));
                    var res = await invoker.RunHttpTriggerAsync(linqPadScriptFileName, "Test", req);
                    Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
                    Assert.AreEqual("hello world", await res.Content.ReadAsStringAsync());
                }

                // async request handler
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/api/test-async"));
                    var res = await invoker.RunHttpTriggerAsync(linqPadScriptFileName, "TestAsync", req);
                    Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
                    Assert.AreEqual("hello world asynchronous", await res.Content.ReadAsStringAsync());
                }
            }
        }
    }
}
