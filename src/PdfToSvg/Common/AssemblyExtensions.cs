// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PdfToSvg.Common
{
    internal static class AssemblyExtensions
    {
        public static byte[] GetManifestResourceBytesOrThrow(this Assembly assembly, string name)
        {
            using var stream = assembly.GetManifestResourceStreamOrThrow(name);

            var data = new byte[stream.Length];
            stream.ReadAll(data, 0, data.Length);
            return data;
        }

        public static string GetManifestResourceTextOrThrow(this Assembly assembly, string name, Encoding? encoding = null)
        {
            using var stream = assembly.GetManifestResourceStreamOrThrow(name);

            if (encoding == null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            else
            {
                using var reader = new StreamReader(stream, encoding);
                return reader.ReadToEnd();
            }
        }

        public static Stream GetManifestResourceStreamOrThrow(this Assembly assembly, string name)
        {
            return
                assembly.GetManifestResourceStream(name) ??
                throw new FileNotFoundException("Could not find embedded manifest resource stream " + name + ".");
        }
    }
}
