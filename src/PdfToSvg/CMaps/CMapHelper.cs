// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal static class CMapHelper
    {
        public static string FormatCode(uint code, int codeLength)
        {
            return "<" + code.ToString("X" + (codeLength * 2)) + ">";
        }

        public static string FormatCodeRange(uint fromCode, uint toCode, int codeLength)
        {
            return FormatCode(fromCode, codeLength) + " " + FormatCode(toCode, codeLength);
        }

        public static string? FormatUnicode(string? unicode)
        {
            return unicode == null
                ? null
                : "<" + string.Concat(unicode.Select(ch => ((int)ch).ToString("X4"))) + "> (" + unicode + ")";
        }

        public static string FormatUnicode(uint unicode)
        {
            return "<" + unicode.ToString("X4") + "> (" + ((char)unicode) + ")";
        }
    }
}
