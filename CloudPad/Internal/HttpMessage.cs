using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public static class HttpMessage
    {
        public static async Task SerializeRequest(HttpRequestMessage req, Stream outputStream)
        {
            outputStream.WriteByte(1); // type

            var w = new BinaryWriter(outputStream);

            w.Write(req.Method.Method);
            w.Write(req.RequestUri.AbsoluteUri);

            SerializeHeaders(req.Headers, w);

            var hasContent = req.Content != null;
            w.Write(hasContent);
            if (hasContent)
            {
                await SerializeContent(req.Content, w);
            }
        }

        public static HttpRequestMessage DeserializeRequest(Stream inputStream)
        {
            if (inputStream.ReadByte() != 1)
            {
                throw new InvalidOperationException();
            }

            var r = new BinaryReader(inputStream);

            var method = r.ReadString();
            var requestUri = r.ReadString();

            var req = new HttpRequestMessage(new HttpMethod(method), requestUri);

            DeserializeHeaders(r, req.Headers);

            var hasContent = r.ReadBoolean();
            if (hasContent)
            {
                req.Content = DeserializeContent(r);
            }

            return req;
        }

        public static async Task SerializeResponse(HttpResponseMessage res, Stream outputStream)
        {
            outputStream.WriteByte(2); // type

            var w = new BinaryWriter(outputStream);

            w.Write((int)res.StatusCode);
            w.Write(res.ReasonPhrase); // optional?

            SerializeHeaders(res.Headers, w);

            var hasContent = res.Content != null;
            w.Write(hasContent);
            if (hasContent)
            {
                await SerializeContent(res.Content, w);
            }
        }

        public static HttpResponseMessage DeserializeResponse(Stream inputStream)
        {
            if (inputStream.ReadByte() != 2)
            {
                throw new InvalidOperationException();
            }

            var r = new BinaryReader(inputStream);

            var statusCode = r.ReadInt32();
            var reasonPhrase = r.ReadString();

            var res = new HttpResponseMessage((System.Net.HttpStatusCode)statusCode);
            if (0 < reasonPhrase.Length)
            {
                res.ReasonPhrase = reasonPhrase;
            }

            DeserializeHeaders(r, res.Headers);

            var hasContent = r.ReadBoolean();
            if (hasContent)
            {
                res.Content = DeserializeContent(r);
            }

            return res;
        }

        private static void SerializeHeaders(HttpHeaders headers, BinaryWriter w)
        {
            w.Write(headers.Count());
            foreach (var header in headers)
            {
                w.Write(header.Key);
                w.Write(header.Value.Count());
                foreach (var v in header.Value)
                {
                    w.Write(v);
                }
            }
        }

        private static void DeserializeHeaders(BinaryReader r, HttpHeaders headers)
        {
            var n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                var key = r.ReadString();
                var c = r.ReadInt32();
                var vs = new string[c];
                for (int j = 0; j < c; j++)
                {
                    vs[j] = r.ReadString();
                }
                headers.TryAddWithoutValidation(key, vs);
            }
        }

        private static async Task SerializeContent(HttpContent content, BinaryWriter w)
        {
            var bytes = await content.ReadAsByteArrayAsync();

            w.Write(bytes.Length);
            w.Write(bytes, 0, bytes.Length);

            SerializeHeaders(content.Headers, w);
        }

        private static HttpContent DeserializeContent(BinaryReader r)
        {
            var contentLength = r.ReadInt32();
            var contentBytes = r.ReadBytes(contentLength);
            var content = new ByteArrayContent(contentBytes, 0, contentLength);
            DeserializeHeaders(r, content.Headers);
            return content;
        }
    }
}
