// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Drawing;
using PdfToSvg.Drawing.Paths;
using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts.Type3
{
    [OperationTarget]
    internal partial class Type3ToCharStringConverter
    {
        private double width;

        private Matrix transform = Matrix.Identity;
        private double currentPointX, currentPointY;
        private readonly PathData currentPath = new PathData();

        private readonly CharStringPath bbox = new();
        private readonly List<CharStringLexeme> content = new();

        private Type3ToCharStringConverter(Matrix transform)
        {
            this.transform = transform;
        }

        public static CharString? Convert(byte[] charProc, Matrix transform, CancellationToken cancellationToken)
        {
            var converter = new Type3ToCharStringConverter(transform);

            using (var contentStream = new MemoryStream(charProc))
            {
                foreach (var op in ContentParser.Parse(contentStream, cancellationToken: cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!Proxy.Invoke(converter, op.Operator, op.Operands))
                    {
                        // Operator that cannot be converted to a char string
                        return null;
                    }
                }
            }

            converter.content.Add(CharStringLexeme.Operator(CharStringOpCode.EndChar));

            var charStringInfo = new CharStringInfo
            {
                Path = converter.bbox,
                Content = converter.content.ToList(),
                Width = converter.width,
            };

            return new CharString(charStringInfo);
        }

        [Operation("d1")]
        private void d1_SetBoundingBox(double wx, double wy, double llx, double lly, double urx, double ury)
        {
            var point = new Point(wx, 0);
            var transformedPoint = transform * point;

            width = transformedPoint.X;
        }

        [Operation("cm")]
        private void cm_SetMatrix(double a, double b, double c, double d, double e, double f)
        {
            transform = new Matrix(a, b, c, d, e, f) * transform;
        }

        [Operation("f")]
        private void f_Fill_NonZero()
        {
            var path = this.currentPath;
            var transform = this.transform;

            for (var i = 0; i < path.Count; i++)
            {
                var command = path[i].Transform(transform);

                switch (command.Type)
                {
                    case PathCommandType.MoveTo:
                        {
                            var dx = command.X - bbox.LastX;
                            var dy = command.Y - bbox.LastY;

                            bbox.RMoveTo(dx, dy);

                            content.Add(CharStringLexeme.Operand(dx));
                            content.Add(CharStringLexeme.Operand(dy));
                            content.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));
                        }
                        break;

                    case PathCommandType.LineTo:
                        {
                            var dx = command.X - bbox.LastX;
                            var dy = command.Y - bbox.LastY;

                            bbox.RLineTo(dx, dy);

                            content.Add(CharStringLexeme.Operand(dx));
                            content.Add(CharStringLexeme.Operand(dy));
                            content.Add(CharStringLexeme.Operator(CharStringOpCode.RLineTo));
                        }
                        break;

                    case PathCommandType.CurveTo:
                        {
                            var p2 = path[i + 2].Transform(transform);
                            var p1 = path[i + 1].Transform(transform);
                            i += 2;

                            var dxa = p1.X - bbox.LastX;
                            var dya = p1.Y - bbox.LastY;
                            var dxb = p2.X - p1.X;
                            var dyb = p2.Y - p1.Y;
                            var dxc = command.X - p2.X;
                            var dyc = command.Y - p2.Y;

                            bbox.RRCurveTo(dxa, dya, dxb, dyb, dxc, dyc);

                            content.Add(CharStringLexeme.Operand(dxa));
                            content.Add(CharStringLexeme.Operand(dya));
                            content.Add(CharStringLexeme.Operand(dxb));
                            content.Add(CharStringLexeme.Operand(dyb));
                            content.Add(CharStringLexeme.Operand(dxc));
                            content.Add(CharStringLexeme.Operand(dyc));
                            content.Add(CharStringLexeme.Operator(CharStringOpCode.RRCurveTo));
                        }
                        break;

                    case PathCommandType.ClosePath:
                        break;

                    default:
                        Log.WriteLine("Unknown command type " + command.Type);
                        break;
                }
            }
        }

        [Operation("F")]
        private void F_Fill_NonZero()
        {
            f_Fill_NonZero();
        }

        [Operation("m")]
        private void m_MoveTo(double x, double y)
        {
            currentPath.MoveTo(x, y);

            currentPointX = x;
            currentPointY = y;
        }

        [Operation("l")]
        private void l_LineTo(double x, double y)
        {
            currentPath.LineTo(x, y);

            currentPointX = x;
            currentPointY = y;
        }

        [Operation("c")]
        private void c_Bezier(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            currentPath.CurveTo(x1, y1, x2, y2, x3, y3);

            currentPointX = x3;
            currentPointY = y3;
        }

        [Operation("v")]
        private void v_Bezier(double x2, double y2, double x3, double y3)
        {
            currentPath.CurveTo(currentPointX, currentPointY, x2, y2, x3, y3);

            currentPointX = x3;
            currentPointY = y3;
        }

        [Operation("y")]
        private void y_Bezier(double x1, double y1, double x3, double y3)
        {
            currentPath.CurveTo(x1, y1, x3, y3, x3, y3);

            currentPointX = x3;
            currentPointY = y3;
        }

        [Operation("h")]
        private void h_ClosePath()
        {
            currentPath.ClosePath();
        }

        [Operation("re")]
        private void re_Rectangle(double x, double y, double width, double height)
        {
            currentPath.MoveTo(x, y);
            currentPath.LineTo(x + width, y);
            currentPath.LineTo(x + width, y + height);
            currentPath.LineTo(x, y + height);
            currentPath.ClosePath();

            currentPointX = x;
            currentPointY = y;
        }

        [Operation("CS")]
        [Operation("cs")]
        [Operation("SC")]
        [Operation("sc")]
        [Operation("SCN")]
        [Operation("scn")]
        [Operation("G")]
        [Operation("g")]
        [Operation("RG")]
        [Operation("rg")]
        [Operation("K")]
        [Operation("k")]
        private void IgnoredColorOperators() { }
    }
}
