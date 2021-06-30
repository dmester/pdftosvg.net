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
    internal struct Rectangle
    {
        public Rectangle(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }

        public Point TopLeft => new Point(X1, Y1);
        public Point TopRight => new Point(X2, Y1);
        public Point BottomLeft => new Point(X1, Y2);
        public Point BottomRight => new Point(X2, Y2);

        public double Width => X2 - X1;
        public double Height => Y2 - Y1;

        public bool Contains(Point pt)
        {
            return pt.X >= X1 && pt.X <= X2 && pt.Y >= Y1 && pt.Y <= Y2;
        }

        public bool Contains(double x, double y)
        {
            return x >= X1 && x <= X2 && y >= Y1 && y <= Y2;
        }

        public bool Contains(Rectangle other)
        {
            return
                other.X1 >= X1 && other.X2 <= X2 &&
                other.Y1 >= Y1 && other.Y2 <= Y2;
        }

        public static Rectangle Intersection(Rectangle a, Rectangle b)
        {
            var x1 = Math.Max(a.X1, b.X1);
            var y1 = Math.Max(a.Y1, b.Y1);
            var x2 = Math.Min(a.X2, b.X2);
            var y2 = Math.Min(a.Y2, b.Y2);

            if (x1 >= x2)
            {
                x1 = x2;
            }

            if (y1 >= y2)
            {
                y1 = y2;
            }

            return new Rectangle(x1, y1, x2, y2);
        }
    }
}
