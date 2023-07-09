// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFont
    {
        public CompactFont(CompactFontSet fontSet)
        {
            FontSet = fontSet;
        }

        public CompactFontSet FontSet { get; }

        public string Name { get; set; } = "Unknown font";

        public bool IsCIDFont => TopDict.ROS.Length > 0;

        public List<int> FDSelect { get; } = new();

        public List<int> CharSet { get; } = new();

        public SingleByteEncoding Encoding { get; set; } = SingleByteEncoding.Standard;

        public List<CompactFontGlyph> Glyphs { get; } = new();

        public CompactFontDict TopDict { get; } = new();

        public CompactFontPrivateDict PrivateDict { get; } = new();

        public List<CharStringSubRoutine> Subrs { get; } = new();

        public List<CompactSubFont> FDArray { get; } = new();
    }
}
