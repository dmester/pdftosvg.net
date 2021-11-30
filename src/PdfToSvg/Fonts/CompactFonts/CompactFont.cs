// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFont
    {
        public byte[] Content { get; set; } = ArrayUtils.Empty<byte>();

        public string Name { get; set; } = "Unknown font";

        public CompactFontStringTable Strings { get; set; }

        public IList<CompactFontGlyph> Glyphs { get; } = new List<CompactFontGlyph>();

        public CompactFontDict TopDict { get; } = new CompactFontDict();

        public CompactFontPrivateDict PrivateDict { get; } = new CompactFontPrivateDict();
    }
}
