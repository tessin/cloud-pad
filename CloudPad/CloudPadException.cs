using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad
{
    public class CloudPadException : Exception
    {
        public string RemoteTypeFullName { get; }
        public string RemoteMessage { get; }
        public string RemoteStackTrace { get; }

        public CloudPadException(
            string message,
            string remoteTypeFullName = null,
            string remoteMessage = null,
            string remoteStackTrace = null
        )
            : base(message)
        {
            RemoteTypeFullName = remoteTypeFullName;
            RemoteMessage = remoteMessage;
            RemoteStackTrace = remoteStackTrace;
        }
    }
}
