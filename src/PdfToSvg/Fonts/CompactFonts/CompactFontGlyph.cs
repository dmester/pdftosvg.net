// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontGlyph
    {
        public CompactFontGlyph(CharString charString, string unicode, int glyphIndex, int sid, double width)
        {
            if (unicode.Length == 0)
            {
                unicode = "\uFFFD";
            }

            CharString = charString;

            Unicode = unicode;
            UnicodeCodePoint = Utf16Encoding.DecodeCodePoint(unicode, 0, out var length);
            IsSingleCodePoint = length == unicode.Length;

            GlyphIndex = glyphIndex;
            SID = sid;

            Width = width;
        }

        public CharString CharString { get; }

        public int GlyphIndex { get; }
        public int SID { get; }

        public string Unicode { get; }
        public uint UnicodeCodePoint { get; }

        public bool IsSingleCodePoint { get; }

        public double Width { get; }

        public override string ToString() => Unicode;
    }
}
