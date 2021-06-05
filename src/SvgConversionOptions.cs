// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public class SvgConversionOptions
    {
        public IImageResolver? ImageResolver { get; set; }

        public IFontResolver? FontResolver { get; set; }

        public double MinStrokeWidth { get; set; } = 0.5;
    }
}
