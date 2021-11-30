// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringPath
    {
        private double x, y;
        private bool open;

        public double MinX { get; private set; } = double.MaxValue;
        public double MaxX { get; private set; } = double.MinValue;

        public double MinY { get; private set; } = double.MaxValue;
        public double MaxY { get; private set; } = double.MinValue;

        private void UpdateBbox(double x, double y)
        {
            if (x < MinX) MinX = x;
            if (x > MaxX) MaxX = x;

            if (y < MinY) MinY = y;
            if (y > MaxY) MaxY = y;
        }

        public void RMoveTo(double dx, double dy)
        {
            x += dx;
            y += dy;

            open = false;
        }

        public void RLineTo(double dx, double dy)
        {
            if (!open)
            {
                UpdateBbox(x, y);
            }

            x += dx;
            y += dy;

            UpdateBbox(x, y);

            open = true;
        }

        public void RRCurveTo(double dxa, double dya, double dxb, double dyb, double dxc, double dyc)
        {
            if (!open)
            {
                UpdateBbox(x, y);
            }

            var x1 = x + dxa;
            var y1 = y + dya;

            var x2 = x1 + dxb;
            var y2 = y1 + dyb;

            var x3 = x2 + dxc;
            var y3 = y2 + dyc;

            // According to the OpenType spec 1.9, the glyph bounding box should be the smallest rectangle containing all control points.
            UpdateBbox(x1, y1);
            UpdateBbox(x2, y2);
            UpdateBbox(x3, y3);

            x = x3;
            y = y3;

            open = true;
        }
    }
}
