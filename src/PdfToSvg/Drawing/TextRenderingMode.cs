// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    [Flags]
    internal enum TextRenderingMode
    {
        None = 0,
        Fill = 1,
        Stroke = 2,
        Clip = 4,
    }
}
