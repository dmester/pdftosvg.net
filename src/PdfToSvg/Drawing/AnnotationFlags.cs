// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Drawing
{
    [Flags]
    internal enum AnnotationFlags
    {
        // PDF 2.0 table 167
        Invisible = 1 << 0,
        Hidden = 1 << 1,
        Print = 1 << 2,
        NoZoom = 1 << 3,
        NoRotate = 1 << 4,
        NoView = 1 << 5,
        ReadOnly = 1 << 6,
        Locked = 1 << 7,
        ToggleNoView = 1 << 8,
        LockedContents = 1 << 9,
    }
}
