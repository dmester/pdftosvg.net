// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontGlyph
    {
        public CompactFontGlyph(string unicode, int glyphIndex, double width, double minX, double maxX, double minY, double maxY)
        {
            if (unicode.Length == 0)
            {
                unicode = "\uFFFD";
            }

            Unicode = unicode;
            UnicodeCodePoint = Utf16Encoding.DecodeCodePoint(unicode, 0, out var length);
            IsSingleCodePoint = length == unicode.Length;

            GlyphIndex = glyphIndex;

            Width = width;
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public int GlyphIndex { get; }

        public string Unicode { get; }
        public uint UnicodeCodePoint { get; }

        public bool IsSingleCodePoint { get; }

        public double Width { get; }

        public double MinX { get; }
        public double MaxX { get; }
        public double MinY { get; }
        public double MaxY { get; }

        public override string ToString() => Unicode;
    }
}
