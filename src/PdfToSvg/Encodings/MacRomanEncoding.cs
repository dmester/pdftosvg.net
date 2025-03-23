﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class MacRomanEncoding : SingleByteEncoding
    {
        // Extracted from PDF spec 1.7 page 661-664.
        // Unicode char codes looked up in Adobe Glyph List:
        // https://raw.githubusercontent.com/adobe-type-tools/agl-aglfn/master/glyphlist.txt
        //
        // PDF spec 1.7 page 274 also specifies differences between Mac OS Roman and the PDF Mac Roman.
        //
        // We have filled missing characters from Wikipedia:
        // https://en.wikipedia.org/wiki/Mac_OS_Roman
        //
        // Adobe Reader does not do this. It will only show the characters specified on page 661-664
        // in the spec, but Pdfium and Pdf.js will show missing characters from Mac OS Roman.

        private static readonly string?[] chars = new[]
        {
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 000 - 007
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 010 - 017
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 020 - 027
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 030 - 037
            "\u0020", "\u0021", "\u0022", "\u0023", "\u0024", "\u0025", "\u0026", "\u0027", // 040 - 047
            "\u0028", "\u0029", "\u002a", "\u002b", "\u002c", "\u002d", "\u002e", "\u002f", // 050 - 057
            "\u0030", "\u0031", "\u0032", "\u0033", "\u0034", "\u0035", "\u0036", "\u0037", // 060 - 067
            "\u0038", "\u0039", "\u003a", "\u003b", "\u003c", "\u003d", "\u003e", "\u003f", // 070 - 077
            "\u0040", "\u0041", "\u0042", "\u0043", "\u0044", "\u0045", "\u0046", "\u0047", // 100 - 107
            "\u0048", "\u0049", "\u004a", "\u004b", "\u004c", "\u004d", "\u004e", "\u004f", // 110 - 117
            "\u0050", "\u0051", "\u0052", "\u0053", "\u0054", "\u0055", "\u0056", "\u0057", // 120 - 127
            "\u0058", "\u0059", "\u005a", "\u005b", "\u005c", "\u005d", "\u005e", "\u005f", // 130 - 137
            "\u0060", "\u0061", "\u0062", "\u0063", "\u0064", "\u0065", "\u0066", "\u0067", // 140 - 147
            "\u0068", "\u0069", "\u006a", "\u006b", "\u006c", "\u006d", "\u006e", "\u006f", // 150 - 157
            "\u0070", "\u0071", "\u0072", "\u0073", "\u0074", "\u0075", "\u0076", "\u0077", // 160 - 167
            "\u0078", "\u0079", "\u007a", "\u007b", "\u007c", "\u007d", "\u007e", nullchar, // 170 - 177
            "\u00c4", "\u00c5", "\u00c7", "\u00c9", "\u00d1", "\u00d6", "\u00dc", "\u00e1", // 200 - 207
            "\u00e0", "\u00e2", "\u00e4", "\u00e3", "\u00e5", "\u00e7", "\u00e9", "\u00e8", // 210 - 217
            "\u00ea", "\u00eb", "\u00ed", "\u00ec", "\u00ee", "\u00ef", "\u00f1", "\u00f3", // 220 - 227
            "\u00f2", "\u00f4", "\u00f6", "\u00f5", "\u00fa", "\u00f9", "\u00fb", "\u00fc", // 230 - 237
            "\u2020", "\u00b0", "\u00a2", "\u00a3", "\u00a7", "\u2022", "\u00b6", "\u00df", // 240 - 247
            "\u00ae", "\u00a9", "\u2122", "\u00b4", "\u00a8", "\u2260", "\u00c6", "\u00d8", // 250 - 257
            "\u221e", "\u00b1", "\u2264", "\u2265", "\u00a5", "\u00b5", "\u2202", "\u2211", // 260 - 267
            "\u220f", "\u03c0", "\u222b", "\u00aa", "\u00ba", "\u03a9", "\u00e6", "\u00f8", // 270 - 277
            "\u00bf", "\u00a1", "\u00ac", "\u221a", "\u0192", "\u2248", "\u2206", "\u00ab", // 300 - 307
            // Note 6: 312 is space instead of no break space as in Mac OS Roman
            //                      v
            "\u00bb", "\u2026", "\u0020", "\u00c0", "\u00c3", "\u00d5", "\u0152", "\u0153", // 310 - 317
            "\u2013", "\u2014", "\u201c", "\u201d", "\u2018", "\u2019", "\u00f7", "\u25ca", // 320 - 327
            "\u00ff", "\u0178", "\u2044", "\u00a4", "\u2039", "\u203a", "\ufb01", "\ufb02", // 330 - 337
            "\u2021", "\u00b7", "\u201a", "\u201e", "\u2030", "\u00c2", "\u00ca", "\u00c1", // 340 - 347
            "\u00cb", "\u00c8", "\u00cd", "\u00ce", "\u00cf", "\u00cc", "\u00d3", "\u00d4", // 350 - 357
            "\uf8ff", "\u00d2", "\u00da", "\u00db", "\u00d9", "\u0131", "\u02c6", "\u02dc", // 360 - 367
            "\u00af", "\u02d8", "\u02d9", "\u02da", "\u00b8", "\u02dd", "\u02db", "\u02c7", // 370 - 377
        };

        private static readonly string?[] glyphNames = GetGlyphNameLookup(chars);

        public MacRomanEncoding() : base(chars, glyphNames) { }
    }
}
