// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace PdfToSvg.Tests
{
    internal static class NewLineExtensions
    {
        public static void AppendUnixLine(this StringBuilder builder)
        {
            builder.Append('\n');
        }

        public static void AppendUnixLine(this StringBuilder builder, string line)
        {
            builder.Append(line);
            builder.Append('\n');
        }

        public static void AppendInvariantUnixLine(this StringBuilder builder, string line, params object[] args)
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, line, args);
            builder.Append('\n');
        }

        public static string ToUnixString(this XElement element)
        {
            var writer = new StringWriter { NewLine = "\n" };

            var xmlWriter = new XmlTextWriter(writer)
            {
                Formatting = Formatting.Indented,
            };

            element.WriteTo(xmlWriter);

            return writer.ToString();
        }
    }
}
