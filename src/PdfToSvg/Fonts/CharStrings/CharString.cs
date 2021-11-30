// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharString
    {
        public CharString(double? width, double minx, double maxx, double miny, double maxy)
        {
            Width = width;
            MinX = minx;
            MaxX = maxx;
            MinY = miny;
            MaxY = maxy;
        }

        public double? Width { get; }

        public double MinX { get; }
        public double MaxX { get; }
        public double MinY { get; }
        public double MaxY { get; }
    }
}
