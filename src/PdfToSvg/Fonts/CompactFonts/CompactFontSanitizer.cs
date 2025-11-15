// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
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

            privateDict.BlueValues = EnsureValidBlues(privateDict.BlueValues);
            privateDict.OtherBlues = EnsureValidBlues(privateDict.OtherBlues);
            privateDict.FamilyBlues = EnsureValidBlues(privateDict.FamilyBlues);
            privateDict.FamilyOtherBlues = EnsureValidBlues(privateDict.FamilyOtherBlues);
        }

        private static double[] EnsureValidBlues(double[] values)
        {
            if ((values.Length & 1) != 0)
            {
                // OTS sanitizer only allows blues with even number of entries
                return ArrayUtils.Empty<double>();
            }
            else
            {
                return values;
            }
        }
    }
}
