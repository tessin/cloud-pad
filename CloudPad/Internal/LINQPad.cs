using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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

        public static string Decrypt(string s)
        {
            // see LINQPad.Repository.Decrypt

            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            var name = GetUtilType()?.Assembly?.GetName();
            if (name == null)
            {
                name = AssemblyName.GetAssemblyName(@"C:\Program Files (x86)\LINQPad5\LINQPad.exe");
            }

            try
            {
                return Encoding.UTF8.GetString(
                    ProtectedData.Unprotect(Convert.FromBase64String(s), name.GetPublicKey(), DataProtectionScope.CurrentUser)
                );
            }
            catch
            {
                return "";
            }
        }
    }
}
