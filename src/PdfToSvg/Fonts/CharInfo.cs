// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts
{
    internal class CharInfo
    {
        public const string NotDef = "\ufffd";

        public uint CharCode;

        public string? GlyphName;

        public uint? GlyphIndex;

        public string Unicode = NotDef;

        public override string ToString()
        {
            return $"{CharCode:x4} => {Unicode}";
        }
    }
}
