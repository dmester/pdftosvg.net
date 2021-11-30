// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Enums
{
    internal enum UsagePermission
    {
        Installable = 0,
        Restricted = 2,
        Printable = 4,
        Editable = 8,
    }
}
