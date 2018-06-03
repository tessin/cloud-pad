using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;

namespace CloudPad.Internal
{
    static class Zip
    {
        public static void CreateEntryFromLINQPadFile(this ZipArchive zip, LINQPadFile linqPadFile, string entryName)
        {
            var mem = new MemoryStream();
            linqPadFile.Save(mem);
            mem.Position = 0;

            var entry = zip.CreateEntry(entryName);
            using (var entryStream = entry.Open())
            {
                mem.CopyTo(entryStream);
            }
        }

        public static void CreateEntryFromJson(this ZipArchive zip, object value, string entryName)
        {
            var entry = zip.CreateEntry(entryName);
            using (var entryStream = entry.Open())
            {
                var textWriter = new StreamWriter(entryStream);
                var jsonSerializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                jsonSerializer.Serialize(new JsonTextWriter(textWriter), value);
                textWriter.Flush();
            }
        }
    }
}
