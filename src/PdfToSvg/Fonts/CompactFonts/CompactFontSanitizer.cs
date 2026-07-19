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

            var defaultValues = new CompactFontDict();

            // OTS Sanitizer rejects non-CID fonts having the following operators specified.
            // See https://github.com/khaledhosny/ots/blob/v9.2.0/src/cff.cc#L756
            if (!compactFont.IsCIDFont)
            {
                ResetCIDOperators(compactFont.TopDict, defaultValues);
            }

            // Only Top DICT should be marked with ROS
            foreach (var subFont in compactFont.FDArray)
            {
                ResetCIDOperators(subFont.FontDict, defaultValues);
            }
        }

        private static void ResetCIDOperators(CompactFontDict fontDict, CompactFontDict defaultValues)
        {
            fontDict.ROS = ArrayUtils.Empty<double>();
            fontDict.CIDFontVersion = defaultValues.CIDFontVersion;
            fontDict.CIDFontRevision = defaultValues.CIDFontRevision;
            fontDict.CIDFontType = defaultValues.CIDFontType;
            fontDict.CIDCount = defaultValues.CIDCount;
            fontDict.UIDBase = defaultValues.UIDBase;
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
