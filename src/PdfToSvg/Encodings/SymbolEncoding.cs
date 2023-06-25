// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class SymbolEncoding : SingleByteEncoding
    {
        // Extracted from PDF spec 1.7 page 668-670.
        // Unicode char codes looked up in Adobe Glyph List:
        // https://raw.githubusercontent.com/adobe-type-tools/agl-aglfn/master/glyphlist.txt

        private static readonly string?[] chars = new[]
        {
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            "\u0020", "\u0021", "\u2200", "\u0023", "\u2203", "\u0025", "\u0026", "\u220b",
            "\u0028", "\u0029", "\u2217", "\u002b", "\u002c", "\u2212", "\u002e", "\u002f",
            "\u0030", "\u0031", "\u0032", "\u0033", "\u0034", "\u0035", "\u0036", "\u0037",
            "\u0038", "\u0039", "\u003a", "\u003b", "\u003c", "\u003d", "\u003e", "\u003f",
            "\u2245", "\u0391", "\u0392", "\u03a7", "\u2206", "\u0395", "\u03a6", "\u0393",
            "\u0397", "\u0399", "\u03d1", "\u039a", "\u039b", "\u039c", "\u039d", "\u039f",
            "\u03a0", "\u0398", "\u03a1", "\u03a3", "\u03a4", "\u03a5", "\u03c2", "\u2126",
            "\u039e", "\u03a8", "\u0396", "\u005b", "\u2234", "\u005d", "\u22a5", "\u005f",
            "\uf8e5", "\u03b1", "\u03b2", "\u03c7", "\u03b4", "\u03b5", "\u03c6", "\u03b3",
            "\u03b7", "\u03b9", "\u03d5", "\u03ba", "\u03bb", "\u00b5", "\u03bd", "\u03bf",
            "\u03c0", "\u03b8", "\u03c1", "\u03c3", "\u03c4", "\u03c5", "\u03d6", "\u03c9",
            "\u03be", "\u03c8", "\u03b6", "\u007b", "\u007c", "\u007d", "\u223c", nullchar,
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar,
            "\u20ac", "\u03d2", "\u2032", "\u2264", "\u2044", "\u221e", "\u0192", "\u2663",
            "\u2666", "\u2665", "\u2660", "\u2194", "\u2190", "\u2191", "\u2192", "\u2193",
            "\u00b0", "\u00b1", "\u2033", "\u2265", "\u00d7", "\u221d", "\u2202", "\u2022",
            "\u00f7", "\u2260", "\u2261", "\u2248", "\u2026", "\uf8e6", "\uf8e7", "\u21b5",
            "\u2135", "\u2111", "\u211c", "\u2118", "\u2297", "\u2295", "\u2205", "\u2229",
            "\u222a", "\u2283", "\u2287", "\u2284", "\u2282", "\u2286", "\u2208", "\u2209",
            "\u2220", "\u2207", "\uf6da", "\uf6d9", "\uf6db", "\u220f", "\u221a", "\u22c5",
            "\u00ac", "\u2227", "\u2228", "\u21d4", "\u21d0", "\u21d1", "\u21d2", "\u21d3",
            "\u25ca", "\u2329", "\uf8e8", "\uf8e9", "\uf8ea", "\u2211", "\uf8eb", "\uf8ec",
            "\uf8ed", "\uf8ee", "\uf8ef", "\uf8f0", "\uf8f1", "\uf8f2", "\uf8f3", "\uf8f4",
            nullchar, "\u232a", "\u222b", "\u2320", "\uf8f5", "\u2321", "\uf8f6", "\uf8f7",
            "\uf8f8", "\uf8f9", "\uf8fa", "\uf8fb", "\uf8fc", "\uf8fd", "\uf8fe", nullchar,
        };

        private static readonly string?[] glyphNames = GetGlyphNameLookup(chars);

        public SymbolEncoding() : base(chars, glyphNames) { }
    }
}
