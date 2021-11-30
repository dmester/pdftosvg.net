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
    internal class CompactFontDict
    {
        [CompactFontDictOperator(0)]
        public string? Version { get; set; }

        [CompactFontDictOperator(1)]
        public string? Notice { get; set; }

        [CompactFontDictOperator(12, 0)]
        public string? Copyright { get; set; }

        [CompactFontDictOperator(2)]
        public string? FullName { get; set; }

        [CompactFontDictOperator(3)]
        public string? FamilyName { get; set; }

        [CompactFontDictOperator(4)]
        public string? Weight { get; set; }

        [CompactFontDictOperator(12, 1)]
        public bool IsFixedPitch { get; set; }

        [CompactFontDictOperator(12, 2)]
        public double ItalicAngle { get; set; }

        [CompactFontDictOperator(12, 3)]
        public double UnderlinePosition { get; set; }

        [CompactFontDictOperator(12, 4)]
        public double UnderlineThichness { get; set; }

        [CompactFontDictOperator(12, 5)]
        public int PaintType { get; set; }

        [CompactFontDictOperator(12, 6)]
        public int CharstringType { get; set; } = 2;

        [CompactFontDictOperator(12, 7)]
        public double[] FontMatrix { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(13)]
        public double UniqueID { get; set; }

        [CompactFontDictOperator(5)]
        public double[] FontBBox { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(12, 8)]
        public double StrokeWidth { get; set; }

        [CompactFontDictOperator(14)]
        public int[] XUID { get; set; } = ArrayUtils.Empty<int>();

        [CompactFontDictOperator(15)]
        public int Charset { get; set; }

        [CompactFontDictOperator(16)]
        public int Encoding { get; set; }

        [CompactFontDictOperator(17)]
        public int CharStrings { get; set; }

        [CompactFontDictOperator(18)]
        public int[] Private { get; set; } = ArrayUtils.Empty<int>();

        [CompactFontDictOperator(12, 20)]
        public double SynthenticBase { get; set; }

        [CompactFontDictOperator(12, 21)]
        public string? PostScript { get; set; }

        [CompactFontDictOperator(12, 22)]
        public string? BaseFontName { get; set; }

        [CompactFontDictOperator(12, 23)]
        public double[] BaseFontBlend { get; set; } = ArrayUtils.Empty<double>();


        // CIDFont Operator Extensions

        [CompactFontDictOperator(12, 30)]
        public double[] ROS { get; set; } = ArrayUtils.Empty<double>();

        [CompactFontDictOperator(12, 31)]
        public int CIDFontVersion { get; set; }

        [CompactFontDictOperator(12, 32)]
        public int CIDFontRevision { get; set; }

        [CompactFontDictOperator(12, 33)]
        public int CIDFontType { get; set; }

        [CompactFontDictOperator(12, 34)]
        public int CIDCount { get; set; } = 8720;

        [CompactFontDictOperator(12, 35)]
        public double UIDBase { get; set; }

        [CompactFontDictOperator(12, 36)]
        public int? FDArray { get; set; }

        [CompactFontDictOperator(12, 37)]
        public int? FDSelect { get; set; }

        [CompactFontDictOperator(12, 38)]
        public string? FontName { get; set; }
    }
}
