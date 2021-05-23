using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class Log
    {
        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }

        public static void WriteLine(string format, params object[] args)
        {
            Debug.WriteLine(format, args);
        }

        public static string TypeOf(object value)
        {
            return value == null ? "(null)" : value.GetType().FullName;
        }
    }
}
