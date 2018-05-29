using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    public class Result
    {
        public Guid CorrelationId { get; set; }

        public string Text { get; set; }
    }
}
