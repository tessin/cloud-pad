using CloudPad.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudPad
{
    [TestClass]
    public class HttpTriggerTests
    {
        class UserQuery1
        {
            // ...in the order in which they are defined
            public string[] Names { get; }

            public UserQuery1()
            {
                Names = new[] {
                    nameof(Test1),
                    nameof(Test2),
                    nameof(Test3),
                    nameof(Test4),
                    nameof(Test5),
                    nameof(Test6),
                    nameof(Test7)
                };
            }

            [HttpTrigger]
            HttpResponseMessage Test1(HttpRequestMessage req)
            {
                Assert.IsNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            [HttpTrigger]
            private HttpResponseMessage Test2(HttpRequestMessage req)
            {
                Assert.IsNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            [HttpTrigger]
            protected HttpResponseMessage Test3(HttpRequestMessage req)
            {
                Assert.IsNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            [HttpTrigger]
            internal HttpResponseMessage Test4(HttpRequestMessage req)
            {
                Assert.IsNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            [HttpTrigger]
            protected internal HttpResponseMessage Test5(HttpRequestMessage req)
            {
                Assert.IsNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            [HttpTrigger]
            public HttpResponseMessage Test6(HttpRequestMessage req)
            {
                Assert.IsNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            [HttpTrigger]
            static HttpResponseMessage Test7(HttpRequestMessage req) // static is not supported
            {
                Assert.IsNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }
        }

        [TestMethod]
        public void HttpTrigger_Method_Visibility_Test()
        {
            var q = new UserQuery1();
            var functionIndex = new FunctionIndex(q);
            functionIndex.Initialize();
            Assert.AreEqual(6, functionIndex.Functions.Count);
            for (int i = 0; i < 6; i++)
            {
                var f = functionIndex.Functions[i];
                Assert.AreEqual(q.Names[i], f.Name);
                var res = ((Task<HttpResponseMessage>)f.InvokeAsync()).GetAwaiter().GetResult();
                Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
            }
        }

        class UserQuery2
        {
            [HttpTrigger]
            public HttpResponseMessage Test1(HttpRequestMessage req)
            {
                Assert.IsNotNull(req);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            [HttpTrigger]
            public Task<HttpResponseMessage> Test2(HttpRequestMessage req)
            {
                Assert.IsNotNull(req);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }

            [HttpTrigger]
            public Task<HttpResponseMessage> Test3(HttpRequestMessage req, ITraceWriter log)
            {
                Assert.IsNotNull(req);
                Assert.IsNotNull(log);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }

            [HttpTrigger]
            public Task<HttpResponseMessage> Test4(HttpRequestMessage req, ITraceWriter log, CancellationToken cancellationToken)
            {
                Assert.IsNotNull(req);
                Assert.IsNotNull(log);
                Assert.IsTrue(cancellationToken.CanBeCanceled);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }

        [TestMethod]
        public void HttpTrigger_Method_Parameter_Type_Test()
        {
            var functionIndex = new FunctionIndex(new UserQuery2());
            functionIndex.Initialize();
            Assert.AreEqual(4, functionIndex.Functions.Count);

            {
                var res = ((Task<HttpResponseMessage>)functionIndex.Functions[0].InvokeAsync(req: new HttpRequestMessage())).GetAwaiter().GetResult();
                Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
            }

            {
                var res = ((Task<HttpResponseMessage>)functionIndex.Functions[1].InvokeAsync(req: new HttpRequestMessage())).GetAwaiter().GetResult();
                Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
            }

            var traceWriter = new Mock<ITraceWriter>();

            {
                var res = ((Task<HttpResponseMessage>)functionIndex.Functions[1].InvokeAsync(req: new HttpRequestMessage(), log: traceWriter.Object)).GetAwaiter().GetResult();
                Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
            }

            using (var cts = new CancellationTokenSource())
            {
                var res = ((Task<HttpResponseMessage>)functionIndex.Functions[1].InvokeAsync(req: new HttpRequestMessage(), log: traceWriter.Object, cancellationToken: cts.Token)).GetAwaiter().GetResult();
                Assert.AreEqual(System.Net.HttpStatusCode.OK, res.StatusCode);
            }
        }
    }
}
