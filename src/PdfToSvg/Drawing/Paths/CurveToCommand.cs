// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing.Paths
{
    internal class CurveToCommand : PathCommand, IMovingCommand
    {
        public CurveToCommand(
            double x1, double y1,
            double x2, double y2,
            double x3, double y3)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            X3 = x3;
            Y3 = y3;
        }

        public double X1 { get; }
        public double Y1 { get; }

        public double X2 { get; }
        public double Y2 { get; }

        public double X3 { get; }
        public double Y3 { get; }

        double IMovingCommand.X => X3;
        double IMovingCommand.Y => Y3;

        public override PathCommand Transform(Matrix matrix)
        {
            var pt1 = matrix * new Point(X1, Y1);
            var pt2 = matrix * new Point(X2, Y2);
            var pt3 = matrix * new Point(X3, Y3);

            return new CurveToCommand(
                pt1.X, pt1.Y,
                pt2.X, pt2.Y,
                pt3.X, pt3.Y
                );
        }

        public override string ToString()
        {
            return string.Format(
                "C {0:0.####} {1:0.####}, {2:0.####} {3:0.####}, {4:0.####} {5:0.####}",
                X1, Y1, X2, Y2, X3, Y3);
        }
    }
}
