// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Model
{
    internal class JbigRegionSegmentInfo
    {
        public int Width;
        public int Height;
        public int X;
        public int Y;

        public JbigCombinationOperator CombinationOperator;

        public bool ColorExtension;

        public override string ToString()
        {
            return $"Region {Width}x{Height} -> ({X},{Y})  Combination: {CombinationOperator}";
        }
    }
}
