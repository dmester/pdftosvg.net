// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    internal class JpxCodeBlockSegment
    {
        public int CodingPasses;
        public bool Terminated;
        public JpxCodeBlockSegmentData Data;
    }
}
