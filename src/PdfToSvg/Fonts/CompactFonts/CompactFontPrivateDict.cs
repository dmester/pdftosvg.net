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
    internal class CompactFontPrivateDict
    {
        [CompactFontDictOperator(6)]
        public double[] BlueValues { get; set; } = ArrayUtils.Empty<double>();  

        [CompactFontDictOperator(7)]
        public double[] OtherBlues { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(8)]
        public double[] FamilyBlues { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(9)]
        public double[] FamilyOtherBlues { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(12, 9)]
        public double BlueScale { get; set; } = 0.039625;

        [CompactFontDictOperator(12, 10)]
        public double BlueShift { get; set; } = 7;

        [CompactFontDictOperator(12, 11)]
        public double BlueFuzz { get; set; } = 1;

        [CompactFontDictOperator(10)]
        public double? StdHW { get; set; }

        [CompactFontDictOperator(11)]
        public double? StdVW { get; set; }

        [CompactFontDictOperator(12, 12)]
        public double[] StemSnapH { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(12, 13)]
        public double[] StemSnapV { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(12, 14)]
        public bool ForceBold { get; set; }

        [CompactFontDictOperator(12, 17)]
        public double LanguageGroup { get; set; }

        [CompactFontDictOperator(12, 18)]
        public double ExpansionFactor { get; set; } = 0.06;

        [CompactFontDictOperator(12, 19)]
        public double InitialRandomSeed { get; set; }

        [CompactFontDictOperator(19)]
        public int? Subrs { get; set; }

        [CompactFontDictOperator(20)]
        public double DefaultWidthX { get; set; }

        [CompactFontDictOperator(21)]
        public double NominalWidthX { get; set; }
    }
}
