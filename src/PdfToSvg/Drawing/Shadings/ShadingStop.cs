// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Drawing.Shadings
{
    internal struct ShadingStop
    {
        public double Offset { get; }
        public RgbColor Color { get; }

        public ShadingStop(double offset, RgbColor color)
        {
            Offset = offset;
            Color = color;
        }
    }
}
