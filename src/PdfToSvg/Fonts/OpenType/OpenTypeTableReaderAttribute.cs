// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class OpenTypeTableReaderAttribute : Attribute
    {
        public OpenTypeTableReaderAttribute(string? tag = null)
        {
            Tag = tag;
        }

        public string? Tag { get; }
    }
}
