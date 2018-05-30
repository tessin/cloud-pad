using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace CloudPad
{
    class HttpServer : IDisposable
    {
        private readonly HttpListener _httpListener;

        public HttpConfiguration Configuration { get; } = new HttpConfiguration();

        private static string GetUriPrefix()
        {
            return Environment.GetEnvironmentVariable("CLOUD_PAD_URI_PREFIX", EnvironmentVariableTarget.Process) ?? "http://localhost:8080/";
        }

        public HttpServer()
        {
            var uriPrefix = GetUriPrefix();

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(uriPrefix);
        }

        private static async Task<HttpRequestMessage> CreateRequestMessageAsync(HttpListenerRequest req)
        {
            var msg = new HttpRequestMessage(new HttpMethod(req.HttpMethod), req.Url);

            var buffer = new MemoryStream();
            await req.InputStream.CopyToAsync(buffer);
            if (buffer.TryGetBuffer(out var slice))
            {
                msg.Content = new ByteArrayContent(slice.Array, slice.Offset, slice.Count);
            }
            else
            {
                throw new NotSupportedException();
            }

            foreach (string headerName in req.Headers)
            {
                string[] headerValues = req.Headers.GetValues(headerName);
                if (!msg.Headers.TryAddWithoutValidation(headerName, headerValues))
                {
                    msg.Content.Headers.TryAddWithoutValidation(headerName, headerValues);
                }
            }

            return msg;
        }

        private static async Task CreateResponseAsync(HttpResponseMessage msg, HttpListenerResponse res)
        {
            res.StatusCode = (int)msg.StatusCode;
            res.StatusDescription = msg.ReasonPhrase;

            foreach (var header in msg.Headers)
            {
                foreach (var v in header.Value)
                {
                    res.AppendHeader(header.Key, v);
                }
            }

            if (msg.Content != null)
            {
                foreach (var header in msg.Content.Headers)
                {
                    foreach (var v in header.Value)
                    {
                        res.AppendHeader(header.Key, v);
                    }
                }

                await msg.Content.CopyToAsync(res.OutputStream);
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // only when running locally as a LINQPad script

            Configuration.EnsureInitialized();

            _httpListener.Start();

            var uriPrefix = new Uri(GetUriPrefix());

            foreach (var route in Configuration.Routes)
            {
                Log.Trace.Append(new Uri(uriPrefix, route.RouteTemplate).AbsoluteUri);
            }

            using (cancellationToken.Register(() => _httpListener.Stop()))
            {
                for (; ; )
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var context = await _httpListener.GetContextAsync();

                    try
                    {
                        var req = await CreateRequestMessageAsync(context.Request);
                        var res = await ProcessAsync(req, cancellationToken);
                        await CreateResponseAsync(res, context.Response);
                    }
                    catch (Exception ex)
                    {
                        var res = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                        res.Content = new StringContent(ex.Message);
                        await CreateResponseAsync(res, context.Response);
                    }
                    finally
                    {
                        context.Response.Close();
                    }
                }
            }
        }

        public async Task<HttpResponseMessage> ProcessAsync(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            // when running locally as a LINQPad script and when running as a Azure function

            Configuration.EnsureInitialized();

            Log.Debug.Append($"incoming request {req.Method} {req.RequestUri.AbsoluteUri}");

            var routeData = Configuration.Routes.GetRouteData(req);
            if (routeData == null)
            {
                Log.Debug.Append($"route not found");

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            req.SetRequestContext(new System.Web.Http.Controllers.HttpRequestContext
            {
                Configuration = Configuration,
                RouteData = routeData
            });

            var invoker = new HttpMessageInvoker(routeData.Route.Handler, false);
            return await invoker.SendAsync(req, cancellationToken);
        }

        public void Dispose()
        {
            _httpListener.Close();
        }
    }
}
