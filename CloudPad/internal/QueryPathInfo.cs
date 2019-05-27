using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CloudPad.Internal
{
    class QueryPathInfo
    {
        public string QueryPath { get; }
        public string QueryDirectoryName { get; }
        public string QueryFileName { get; }
        public string QueryFileNameWithoutExtension { get; }

        private static string GetInstanceId(string queryPath)
        {
            var hasher = new SHA256Managed();
            var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(queryPath.ToLowerInvariant()));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public string InstanceId { get; }

        public QueryPathInfo(string queryPath)
        {
            if (queryPath == null)
            {
                throw new ArgumentNullException(nameof(queryPath));
            }

            this.QueryPath = queryPath;
            this.QueryDirectoryName = Path.GetDirectoryName(queryPath);
            this.QueryFileName = Path.GetFileName(queryPath);
            this.QueryFileNameWithoutExtension = Path.GetFileNameWithoutExtension(queryPath);

            this.InstanceId = GetInstanceId(queryPath);
        }
    }
}
