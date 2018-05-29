using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public static class ChannelExtensions
    {
        public static void Send(this Stream outputStream, object message)
        {
            var buffer = new MemoryStream();
            var w = new BinaryWriter(buffer);
            w.Write(0); // reserve header size
            var json = JsonConvert.SerializeObject(message);
            w.Write(json);
            if (buffer.TryGetBuffer(out var slice))
            {
                // fix header
                buffer.Position = 0;
                w.Write(slice.Count - 4); // message size - header size
                outputStream.Write(slice.Array, slice.Offset, slice.Count);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static async Task<T> ReceiveAsync<T>(this Stream inputStream)
            where T : class, new()
        {
            var bytes = new byte[4];
            if (await inputStream.ReadAsync(bytes, 0, 4) < 4)
            {
                return null;
            }
            var size = BitConverter.ToInt32(bytes, 0);
            var bytes2 = new byte[size];
            if (await inputStream.ReadAsync(bytes2, 0, size) < size)
            {
                return null;
            }
            var buffer = new MemoryStream(bytes2);
            var r = new BinaryReader(buffer);
            var json = r.ReadString();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
