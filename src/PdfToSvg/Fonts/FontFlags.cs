// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts
{
    // ISO 32000-2 table 121
    [Flags]
    internal enum FontFlags
    {
        FixedPitch = 1 << 0,
        Serif = 1 << 1,
        Symbolic = 1 << 2,
        Script = 1 << 3,
        Nonsymbolic = 1 << 5,
        Italic = 1 << 6,
        AllCap = 1 << 16,
        SmallCap = 1 << 17,
        ForceBold = 1 << 18,
    }
}
