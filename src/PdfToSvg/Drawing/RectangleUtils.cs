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
    internal static class RectangleUtils
    {
        public static Rectangle GetA4()
        {
            return new Rectangle(0, 0, 594.96, 841.92);
        }

        public static Rectangle GetBoundingRectangle(this Point[] points)
        {
            var minX = double.MaxValue;
            var maxX = double.MinValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;

            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];
                if (minX > point.X) minX = point.X;
                if (maxX < point.X) maxX = point.X;
                if (minY > point.Y) minY = point.Y;
                if (maxY < point.Y) maxY = point.Y;
            }

            if (minX == double.MaxValue)
            {
                return new Rectangle();
            }

            return new Rectangle(minX, minY, maxX, maxY);
        }

        public static Rectangle GetBoundingRectangle(this IEnumerable<Point> points)
        {
            var minX = double.MaxValue;
            var maxX = double.MinValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;

            foreach (var point in points)
            {
                if (minX > point.X) minX = point.X;
                if (maxX < point.X) maxX = point.X;
                if (minY > point.Y) minY = point.Y;
                if (maxY < point.Y) maxY = point.Y;
            }

            if (minX == double.MaxValue)
            {
                return new Rectangle();
            }

            return new Rectangle(minX, minY, maxX, maxY);
        }

        public static Rectangle GetBoundingRectangleAfterTransform(Rectangle rect, Matrix transform)
        {
            if (transform.IsIdentity)
            {
                return rect;
            }

            return (transform * rect).GetBoundingRectangle();
        }
    }
}
