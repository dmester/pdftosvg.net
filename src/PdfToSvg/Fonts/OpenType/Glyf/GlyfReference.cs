// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Fonts.OpenType.Glyf
{
    internal struct GlyfReference
    {
        public ushort GlyphIndex;
        public ComponentGlyphFlags Flags;
        public ArraySegment<byte> Data;
    }
}
