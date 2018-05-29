using System;

namespace CloudPad.Internal
{
    public class Envelope
    {
        public static Envelope Create(
            string linqPadScriptFileName,
            string methodName,
            string[] args
            )
        {
            return new Envelope
            {
                CorrelationId = Guid.NewGuid(),

                LINQPadScriptFileName = linqPadScriptFileName,
                MethodName = methodName,
                Args = args
            };
        }

        public Guid CorrelationId { get; set; }

        public string LINQPadScriptFileName { get; set; }
        public string MethodName { get; set; }
        public string[] Args { get; set; }
    }
}
