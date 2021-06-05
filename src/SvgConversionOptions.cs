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

        /// <summary>
        /// Spacing between letters below this threshold is assumed to be kerning and removed.
        /// The value is relative to the current font size, where 1.0 represents the font size.
        /// </summary>
        public double KerningThreshold { get; set; } = 0.2;
    }
}
