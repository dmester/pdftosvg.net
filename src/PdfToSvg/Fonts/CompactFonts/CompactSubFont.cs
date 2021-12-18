// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactSubFont
    {
        public CompactFontDict FontDict { get; } = new();

        public CompactFontPrivateDict PrivateDict { get; } = new();

        public List<CharStringSubRoutine> Subrs { get; set; } = new();
    }
}
