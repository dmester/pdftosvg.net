// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Encodings
{
    internal class Type1Encoding : SingleByteEncoding
    {
        public Type1Encoding(string?[] glyphNames) : base(GetUnicodeLookup(glyphNames), glyphNames)
        {
        }
    }
}
