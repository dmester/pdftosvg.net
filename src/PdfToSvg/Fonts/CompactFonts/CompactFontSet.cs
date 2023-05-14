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
    internal class CompactFontSet
    {
        public IList<CompactFont> Fonts { get; } = new List<CompactFont>();

        public CompactFontStringTable Strings { get; set; } = new CompactFontStringTable();

        public IList<CharStringSubRoutine> Subrs { get; } = new List<CharStringSubRoutine>();
    }
}
