// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Encodings
{
    internal class CompactFontEncoding : SingleByteEncoding
    {
        public CompactFontEncoding(string?[] toUnicode, string?[] toGlyphNames) : base(toUnicode, toGlyphNames)
        {
        }
    }
}
