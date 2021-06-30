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
    internal class TextSpan
    {
        public string Value;
        public double Width;
        public double SpaceBefore;
        public GraphicsState Style;

        public TextSpan(double spaceBefore, GraphicsState style, string value, double width)
        {
            SpaceBefore = spaceBefore;
            Style = style;
            Value = value;
            Width = width;
        }
    }
}
