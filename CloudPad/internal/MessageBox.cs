using System;
using System.Reflection;

namespace CloudPad.Internal
{
    static class MessageBox
    {
        private static readonly MethodInfo _show;

        static MessageBox()
        {
            var messageBoxType = Type.GetType("System.Windows.Forms.MessageBox, System.Windows.Forms");

            _show = messageBoxType.GetMethod("Show", BindingFlags.Public | BindingFlags.Static, null, new[] {
                typeof(string),
                typeof(string),
                Type.GetType("System.Windows.Forms.MessageBoxButtons, System.Windows.Forms"),
                Type.GetType("System.Windows.Forms.MessageBoxIcon, System.Windows.Forms"),
            }, null);
        }

        public static bool ShowYesNoQuestion(string text, string caption)
        {
            var result = _show.Invoke(null, new object[] {
                text,
                caption,
                4, // YesNo
                0x20 // Question
            });

            return Convert.ToInt32(result) == 6;
        }
    }
}
