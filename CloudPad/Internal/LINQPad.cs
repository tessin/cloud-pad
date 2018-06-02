using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudPad.Internal
{
    class LINQPad
    {
        private static Type GetUtilType()
        {
            return Type.GetType("LINQPad.Util, LINQPad", false);
        }

        public static EventHandler RegisterCleanup(EventHandler handler)
        {
            var utilType = GetUtilType();
            if (utilType != null)
            {
                utilType.GetEvent("Cleanup").AddEventHandler(null, handler);
            }
            return handler;
        }

        public static EventHandler UnregisterCleanup(EventHandler handler)
        {
            var utilType = GetUtilType();
            if (utilType != null)
            {
                utilType.GetEvent("Cleanup").RemoveEventHandler(null, handler);
            }
            return handler;
        }

        public static string GetCurrentQueryPath()
        {
            var utilType = GetUtilType();
            if (utilType != null)
            {
                return utilType.GetProperty("CurrentQueryPath")?.GetValue(null) as string;
            }
            return null;
        }
    }
}
