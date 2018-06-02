using System;

namespace CloudPad.Internal
{
    public class Envelope
    {
        public static Envelope Create(
            string linqPadScriptFileName,
            string[] args
            )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new Envelope
            {
                CorrelationId = Guid.NewGuid(),

                LINQPadScriptFileName = linqPadScriptFileName,
                Args = args
            };
        }

        public Guid CorrelationId { get; set; }

        public string LINQPadScriptFileName { get; set; }
        public string[] Args { get; set; }
    }
}
