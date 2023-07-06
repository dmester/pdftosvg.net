// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal static class CompactFontSanitizer
    {
        public static void Sanitize(CompactFont compactFont)
        {
            compactFont.Name ??= "Untitled";
            Sanitize(compactFont.PrivateDict);
        }

        public static void Sanitize(CompactFontSet compactFontSet)
        {
            foreach (var compactFont in compactFontSet.Fonts)
            {
                Sanitize(compactFont);
            }
        }

        private static void Sanitize(CompactFontPrivateDict privateDict)
        {
            // See issue 6. The font renderer in Chrome and Firefox on Windows will refuse loading the font when the
            // expansion factor is 0. Reset it to the default value in this case.
            if (privateDict.ExpansionFactor == 0)
            {
                privateDict.ExpansionFactor = 0.06;
            }
        }
    }
}
