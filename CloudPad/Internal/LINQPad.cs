using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    class LINQPad
    {
        public static EventHandler RegisterCleanup(EventHandler handler)
        {
            var utilType = Type.GetType("LINQPad.Util, LINQPad", false);
            if (utilType != null)
            {
                utilType.GetEvent("Cleanup").AddEventHandler(null, handler);
            }
            return handler;
        }

        public static EventHandler UnregisterCleanup(EventHandler handler)
        {
            var utilType = Type.GetType("LINQPad.Util, LINQPad", false);
            if (utilType != null)
            {
                utilType.GetEvent("Cleanup").RemoveEventHandler(null, handler);
            }
            return handler;
        }
    }
}
