// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Enums
{
    internal enum SfntVersion : uint
    {
        TrueType = 0x00010000u,
        Cff = 0x4F54544Fu,
        True = 0x74727565u,
        Typ1 = 0x74797031u,
    }
}
