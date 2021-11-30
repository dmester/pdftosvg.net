// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Enums
{
    [Flags]
    internal enum MacStyle
    {
        None = 0,
        Bold = 1 << 0,
        Italic = 1 << 1,
        Underline = 1 << 2,
        Outline = 1 << 3,
        Shadow = 1 << 4,
        Condensed = 1 << 5,
        Extended = 1 << 6,
    }
}
