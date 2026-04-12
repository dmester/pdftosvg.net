// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Glyf
{
    // Implementation note:
    // The full glyph models are not kept during the lifetime of the font, to lower the memory consumption.
    internal struct FullGlyfRecord
    {
        // Pointer fields first for better alignment
        private ArraySegment<byte> data;

        // For complex glyphs
        public GlyfReference[]? ReferencedGlyphs;

        // Shared
        public short NumberOfContours;
        public ushort DataLength;
        public ushort NumberOfPoints;
        public ushort NumInstructions;

        public short XMin;
        public short YMin;
        public short XMax;
        public short YMax;

        public ArraySegment<byte> SimpleContent
        {
            get => data;
            set => data = value;
        }
        public ArraySegment<byte> CompositeInstructions
        {
            get => data;
            set => data = value;
        }

        public bool IsSimpleGlyph => NumberOfContours >= 0;
    }
}
