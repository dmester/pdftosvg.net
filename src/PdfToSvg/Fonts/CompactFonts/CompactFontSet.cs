// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontSet
    {
        public CompactFontSet(IList<CompactFont> fonts)
        {
            Fonts = fonts;
        }

        public IList<CompactFont> Fonts { get; }
    }
}
