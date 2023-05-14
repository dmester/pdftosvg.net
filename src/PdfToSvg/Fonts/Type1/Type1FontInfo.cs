// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.Type1
{
    internal class Type1FontInfo
    {
        public int lenIV = 4;
        public string? Notice;
        public string? FullName;
        public string? FamilyName;
        public string? Weight;
        public double ItalicAngle;
        public bool isFixedPitch;
        public double UnderlinePosition;
        public double UnderlineThickness;
        public string? FontName;
        public int PaintType;
        public int WMode;
        public double[]? FontBBox;
        public int FontType;
        public double[]? FontMatrix;
        public SingleByteEncoding? Encoding;

        public double[]? BlueValues;
        public double BlueScale;
        public double[]? StdHW;
        public double[]? StdVW;
        public double[]? StemSnapH;
        public IList<CharStringSubRoutine>? Subrs;

        public Dictionary<string, CharString>? CharStrings;
    }
}
