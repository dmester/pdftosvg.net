// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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
        // TODO add context

        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }

        public static void WriteLine(string format, params object[] args)
        {
            Debug.WriteLine(format, args);
        }

        public static string TypeOf(object? value)
        {
            return value == null ? "(null)" : value.GetType().FullName ?? "unknown";
        }
    }
}
