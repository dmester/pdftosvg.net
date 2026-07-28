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
    internal enum PathCommandType : byte
    {
        MoveTo,
        LineTo,
        CurveTo, // End point
        CurveToP1, // First control point
        CurveToP2, // Second control point
        ClosePath,
    }

    internal readonly struct PathCommand
    {
        private PathCommand(PathCommandType type, double x, double y)
        {
            Type = type;
            X = x;
            Y = y;
        }

        public readonly PathCommandType Type;
        public readonly double X;
        public readonly double Y;

        public static PathCommand ClosePath => new PathCommand(PathCommandType.ClosePath, 0, 0);
        public static PathCommand MoveTo(double x, double y) => new PathCommand(PathCommandType.MoveTo, x, y);
        public static PathCommand LineTo(double x, double y) => new PathCommand(PathCommandType.LineTo, x, y);
        public static PathCommand CurveToP1(double x, double y) => new PathCommand(PathCommandType.CurveToP1, x, y);
        public static PathCommand CurveToP2(double x, double y) => new PathCommand(PathCommandType.CurveToP2, x, y);
        public static PathCommand CurveTo(double x, double y) => new PathCommand(PathCommandType.CurveTo, x, y);

        public PathCommand Transform(Matrix matrix)
        {
            if (Type == PathCommandType.ClosePath)
            {
                return this;
            }

            var pt3 = matrix * new Point(X, Y);
            return new PathCommand(Type, pt3.X, pt3.Y);
        }

        public override string ToString()
        {
            switch (Type)
            {
                case PathCommandType.MoveTo:
                    return string.Format("M {0:0.####} {1:0.####}", X, Y);

                case PathCommandType.LineTo:
                    return string.Format("L {0:0.####} {1:0.####}", X, Y);

                case PathCommandType.CurveTo:
                    return string.Format("C {0:0.####} {1:0.####}", X, Y);

                case PathCommandType.CurveToP1:
                case PathCommandType.CurveToP2:
                    return string.Format("{0:0.####} {1:0.####}", X, Y);

                default:
                    return "z";
            }
        }
    }
}
