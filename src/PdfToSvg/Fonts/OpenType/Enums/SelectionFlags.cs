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
    internal enum SelectionFlags
    {
        None = 0,
        Italic = 1 << 0,
        Underscore = 2 << 1,
        Negative = 1 << 2,
        Outlined = 1 << 3,
        Strikeout = 1 << 4,
        Bold = 1 << 5,
        Regular = 1 << 6,
        UseTypoMetrics = 1 << 7,
        WeightWidthSlope = 1 << 8,
        Oblique = 1 << 9,
    }
}
