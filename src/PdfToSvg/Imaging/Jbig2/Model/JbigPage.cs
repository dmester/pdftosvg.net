// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Model
{
    internal class JbigPage
    {
        public int PageNumber;

        public int Width;
        public int Height;
        public int XResolution;
        public int YResolution;

        public bool MightBeLossless;
        public bool MightContainRefinements;
        public bool DefaultPixelValue;
        public JbigCombinationOperator DefaultCombinationOperator;
        public bool RequiresAuxiliaryBuffers;
        public bool CombinationOperatorOverridden;
        public bool MightContainColouredSegment;

        public bool IsStriped;
        public int MaximumStripeSize;

        public JbigBitmap Bitmap = JbigBitmap.Empty;

        public override string ToString()
        {
            return "Page " + PageNumber + ": " + Width + "x" + Height;
        }
    }
}
