// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal static class ColorSpaceExtensions
    {
        public static RgbColor GetDefaultRgbColor(this ColorSpace colorSpace) => new RgbColor(colorSpace, colorSpace.DefaultColor);
    }
}
