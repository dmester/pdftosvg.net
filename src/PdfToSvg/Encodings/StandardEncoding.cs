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
    internal class StandardEncoding : SingleByteEncoding
    {
        // Extracted from PDF spec 1.7 page 661-664.
        // Unicode char codes looked up in Adobe Glyph List:
        // https://raw.githubusercontent.com/adobe-type-tools/agl-aglfn/master/glyphlist.txt

        private static readonly string?[] chars = new[]
        {
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 000 - 007
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 010 - 017
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 020 - 027
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 030 - 037
            "\u0020", "\u0021", "\u0022", "\u0023", "\u0024", "\u0025", "\u0026", "\u2019", // 040 - 047
            "\u0028", "\u0029", "\u002a", "\u002b", "\u002c", "\u002d", "\u002e", "\u002f", // 050 - 057
            "\u0030", "\u0031", "\u0032", "\u0033", "\u0034", "\u0035", "\u0036", "\u0037", // 060 - 067
            "\u0038", "\u0039", "\u003a", "\u003b", "\u003c", "\u003d", "\u003e", "\u003f", // 070 - 077
            "\u0040", "\u0041", "\u0042", "\u0043", "\u0044", "\u0045", "\u0046", "\u0047", // 100 - 107
            "\u0048", "\u0049", "\u004a", "\u004b", "\u004c", "\u004d", "\u004e", "\u004f", // 110 - 117
            "\u0050", "\u0051", "\u0052", "\u0053", "\u0054", "\u0055", "\u0056", "\u0057", // 120 - 127
            "\u0058", "\u0059", "\u005a", "\u005b", "\u005c", "\u005d", "\u005e", "\u005f", // 130 - 137
            "\u2018", "\u0061", "\u0062", "\u0063", "\u0064", "\u0065", "\u0066", "\u0067", // 140 - 147
            "\u0068", "\u0069", "\u006a", "\u006b", "\u006c", "\u006d", "\u006e", "\u006f", // 150 - 157
            "\u0070", "\u0071", "\u0072", "\u0073", "\u0074", "\u0075", "\u0076", "\u0077", // 160 - 167
            "\u0078", "\u0079", "\u007a", "\u007b", "\u007c", "\u007d", "\u007e", nullchar, // 170 - 177
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 200 - 207
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 210 - 217
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 220 - 227
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 230 - 237
            nullchar, "\u00a1", "\u00a2", "\u00a3", "\u2044", "\u00a5", "\u0192", "\u00a7", // 240 - 247
            "\u00a4", "\u0027", "\u201c", "\u00ab", "\u2039", "\u203a", "\ufb01", "\ufb02", // 250 - 257
            nullchar, "\u2013", "\u2020", "\u2021", "\u00b7", nullchar, "\u00b6", "\u2022", // 260 - 267
            "\u201a", "\u201e", "\u201d", "\u00bb", "\u2026", "\u2030", nullchar, "\u00bf", // 270 - 277
            nullchar, "\u0060", "\u00b4", "\u02c6", "\u02dc", "\u00af", "\u02d8", "\u02d9", // 300 - 307
            "\u00a8", nullchar, "\u02da", "\u00b8", nullchar, "\u02dd", "\u02db", "\u02c7", // 310 - 317
            "\u2014", nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 320 - 327
            nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, nullchar, // 330 - 337
            nullchar, "\u00c6", nullchar, "\u00aa", nullchar, nullchar, nullchar, nullchar, // 340 - 347
            "\u0141", "\u00d8", "\u0152", "\u00ba", nullchar, nullchar, nullchar, nullchar, // 350 - 357
            nullchar, "\u00e6", nullchar, nullchar, nullchar, "\u0131", nullchar, nullchar, // 360 - 367
            "\u0142", "\u00f8", "\u0153", "\u00df", nullchar, nullchar, nullchar, nullchar, // 370 - 377
        };

        private static readonly string?[] glyphNames = GetGlyphNameLookup(chars);

        public StandardEncoding() : base(chars, glyphNames) { }
    }
}
