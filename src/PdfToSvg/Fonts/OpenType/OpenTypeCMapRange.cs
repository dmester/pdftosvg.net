// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeCMapRange
    {
        public OpenTypeCMapRange(uint startUnicode, uint endUnicode, uint startGlyphIndex)
        {
            StartUnicode = startUnicode;
            EndUnicode = endUnicode;
            StartGlyphIndex = startGlyphIndex;
            EndGlyphIndex = startGlyphIndex + (endUnicode - startUnicode);
        }

        public uint StartUnicode { get; }
        public uint EndUnicode { get; }
        public uint StartGlyphIndex { get; }
        public uint EndGlyphIndex { get; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1} <=> {2}", StartUnicode, EndUnicode, StartGlyphIndex);
        }
    }
}
