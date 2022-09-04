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
    internal struct OpenTypeCMapChar
    {
        public OpenTypeCMapChar(uint unicode, uint glyphIndex)
        {
            Unicode = unicode;
            GlyphIndex = glyphIndex;
        }

        public uint Unicode { get; }
        public uint GlyphIndex { get; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} <=> {1}", Unicode, GlyphIndex);
        }
    }
}
