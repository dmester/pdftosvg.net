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
    internal class TextParagraph
    {
        public List<TextSpan> Content = new List<TextSpan>();
        public Matrix Matrix = Matrix.Identity;
        public double X;
        public double Y;
        public bool Visible;
        public bool AppendClipping;
    }
}
