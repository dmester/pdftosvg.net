// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Drawing.Paths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class SvgConversion
    {
        private const string Hex = "0123456789abcdef";

        private static string FormatRgbComponent(float component)
        {
            var intComponent = (int)(component * 255);

            var result = new[]
            {
                Hex[(intComponent >> 4) & 0xf],
                Hex[intComponent & 0xf],
            };

            return new string(result);
        }

        public static string FormatColor(RgbColor color)
        {
            var r = FormatRgbComponent(color.Red);
            var g = FormatRgbComponent(color.Green);
            var b = FormatRgbComponent(color.Blue);

            if (r[0] == r[1] &&
                g[0] == g[1] &&
                b[0] == b[1])
            {
                return "#" + r[0] + g[0] + b[0];
            }

            return "#" + r + g + b;
        }

        public static string FormatCoordinate(double number)
        {
            var result = number.ToString("0.####", CultureInfo.InvariantCulture);

            // .NET Core 3+ formats negative values close to 0 as "-0".
            // This adds no value to the svg.
            if (result == "-0")
            {
                result = "0";
            }

            return result;
        }

        public static string PathData(PathData path)
        {
            var result = new StringBuilder();
            var lastPosition = (IMovingCommand?)null;

            foreach (var command in path)
            {
                switch (command)
                {
                    case MoveToCommand moveTo:
                        result.Append("M" + FormatCoordinate(moveTo.X) + " " + FormatCoordinate(moveTo.Y));
                        break;

                    case LineToCommand lineTo:
                        if (lastPosition == null)
                        {
                            result.Append("L" + FormatCoordinate(lineTo.X) + " " + FormatCoordinate(lineTo.Y));
                        }
                        else if (lastPosition.X == lineTo.X)
                        {
                            result.Append("v" + FormatCoordinate(lineTo.Y - lastPosition.Y));
                        }
                        else if (lastPosition.Y == lineTo.Y)
                        {
                            result.Append("h" + FormatCoordinate(lineTo.X - lastPosition.X));
                        }
                        else
                        {
                            result.Append("l" +
                                FormatCoordinate(lineTo.X - lastPosition.X) + " " +
                                FormatCoordinate(lineTo.Y - lastPosition.Y));
                        }
                        break;

                    case CurveToCommand curveTo:
                        result.Append("C" +
                            FormatCoordinate(curveTo.X1) + " " + FormatCoordinate(curveTo.Y1) + "," +
                            FormatCoordinate(curveTo.X2) + " " + FormatCoordinate(curveTo.Y2) + "," +
                            FormatCoordinate(curveTo.X3) + " " + FormatCoordinate(curveTo.Y3));
                        break;

                    case ClosePathCommand _:
                        result.Append("z");
                        break;

                    default:
                        throw new Exception("Unknown command");
                }

                lastPosition = command as IMovingCommand;
            }

            return result.ToString();
        }

        public static string Matrix(Matrix matrix)
        {
            var a = FormatCoordinate(matrix.A);
            var b = FormatCoordinate(matrix.B);
            var c = FormatCoordinate(matrix.C);
            var d = FormatCoordinate(matrix.D);
            var e = FormatCoordinate(matrix.E);
            var f = FormatCoordinate(matrix.F);

            if (a == "1" && b == "0" && c == "0" && d == "1")
            {
                if (e == "0" && f == "0")
                {
                    return "none";
                }
                else
                {
                    return "translate(" + e + " " + f + ")";
                }
            }

            return "matrix(" + a + " " + b + " " + c + " " + d + " " + e + " " + f + ")";
        }
    }
}
