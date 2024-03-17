// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
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

        private static bool IsInvalidChar(char ch)
        {
            // Invalid XML char according to 
            // https://www.w3.org/TR/REC-xml/#charsets
            return
                ch > '\ufffe' ||
                ch < '\u0020' &&
                ch != '\u0009' &&
                ch != '\u000A' &&
                ch != '\u000D';
        }

        public static string ReplaceInvalidChars(string s, char replacementChar = '\ufffd')
        {
            const char SoftHyphen = '\u00ad';

            var i = 0;

            while (i < s.Length && !IsInvalidChar(s[i]) && s[i] != SoftHyphen)
            {
                i++;
            }

            if (i < s.Length)
            {
                var result = s.ToCharArray();

                for (; i < result.Length; i++)
                {
                    if (result[i] == SoftHyphen)
                    {
                        result[i] = '-';
                    }
                    else if (IsInvalidChar(result[i]))
                    {
                        result[i] = replacementChar;
                    }
                }

                return new string(result);
            }

            return s;
        }

        public static string? FormatFontStyle(FontStyle? fontStyle)
        {
            return fontStyle switch
            {
                FontStyle.Italic => "italic",
                FontStyle.Oblique => "oblique",
                _ => null,
            };
        }

        public static string? FormatFontWeight(FontWeight? fontWeight)
        {
            if (fontWeight == FontWeight.Default || fontWeight == null)
            {
                return null;
            }

            var numericWeight = MathUtils.Clamp(((int)fontWeight + 50) / 100, 1, 9) * 100;

            if (numericWeight == 400)
            {
                return null;
            }

            if (numericWeight == 700)
            {
                return "bold";
            }

            return numericWeight.ToString(CultureInfo.InvariantCulture);
        }

        public static string FormatFontMetric(double number)
        {
            if (number >= 100 || number <= -100)
            {
                return FormatNumber(number, "0");
            }

            if (number >= 10 || number <= -10)
            {
                return FormatNumber(number, "0.##");
            }

            if (number >= 1 || number <= -1)
            {
                return FormatNumber(number, "0.###");
            }

            return FormatNumber(number, "0.####");
        }

        public static string FormatCoordinate(double number)
        {
            return FormatNumber(number, "0.####");
        }

        public static string FormatTextOffset(double number)
        {
            return FormatNumber(number, "0.##");
        }

        public static string FormatBlendMode(BlendMode mode)
        {
            return mode switch
            {
                BlendMode.Normal => "normal",

                BlendMode.Multiply => "multiply",
                BlendMode.Screen => "screen",
                BlendMode.Overlay => "overlay",
                BlendMode.Darken => "darken",
                BlendMode.Lighten => "lighten",
                BlendMode.ColorDodge => "color-dodge",
                BlendMode.ColorBurn => "color-burn",
                BlendMode.HardLight => "hard-light",
                BlendMode.SoftLight => "soft-light",
                BlendMode.Difference => "difference",
                BlendMode.Exclusion => "exclusion",

                BlendMode.Hue => "hue",
                BlendMode.Saturation => "saturation",
                BlendMode.Color => "color",
                BlendMode.Luminosity => "luminosity",

                _ => "normal",
            };
        }

        private static string FormatNumber(double number, string formatString)
        {
            var result = number.ToString(formatString, CultureInfo.InvariantCulture);

            // .NET Core 3+ formats negative values close to 0 as "-0".
            // This adds no value to the svg.
            if (result == "-0")
            {
                result = "0";
            }

            // Remove leading 0 in fractional numbers in the range (-1, 1)
            else if (result.StartsWith("0."))
            {
                result = result.Substring(1);
            }
            else if (result.StartsWith("-0."))
            {
                result = result.Remove(1, 1);
            }

            return result;
        }

        private class PathPoint
        {
            private const int FractionalDigits = 4;

            public readonly Point Original;
            public readonly Point Rounded;
            public readonly Point Diff;

            public PathPoint(PathPoint? previous, double x, double y)
            {
                Original = new Point(x, y);
                
                if (previous == null)
                {
                    Rounded = new Point(
                        Math.Round(x, FractionalDigits),
                        Math.Round(y, FractionalDigits));

                    Diff = Rounded;
                }
                else
                {
                    Diff = new Point(
                        RoundDiff(previous.Rounded.X, previous.Original.X, x),
                        RoundDiff(previous.Rounded.Y, previous.Original.Y, y));

                    Rounded = new Point(
                        previous.Rounded.X + Diff.X,
                        previous.Rounded.Y + Diff.Y);
                }
            }

            private static double RoundDiff(
                double previousRoundedValue, double previousOriginalValue, double newOriginalValue)
            {
                return newOriginalValue == previousOriginalValue
                    ? 0 // If the unrounded values don't differ, don't move the coordinate
                    : Math.Round(newOriginalValue - previousRoundedValue, FractionalDigits);
            }
        }

        public static string PathData(PathData path)
        {
            var result = new StringBuilder();

            PathPoint? previous = null;
            PathPoint? start = null;

            foreach (var command in path)
            {
                var movingCommand = command as IMovingCommand;
                if (movingCommand != null)
                {
                    var dest = new PathPoint(previous, movingCommand.X, movingCommand.Y);

                    switch (command)
                    {
                        case MoveToCommand moveTo:
                            if (previous == null)
                            {
                                result.Append("M" + FormatCoordinate(dest.Diff.X) + " " + FormatCoordinate(dest.Diff.Y));
                            }
                            else if (dest.Diff.X == 0 && dest.Diff.Y == 0)
                            {
                                // Skip noop command
                            }
                            else
                            {
                                result.Append("m" + FormatCoordinate(dest.Diff.X) + " " + FormatCoordinate(dest.Diff.Y));
                            }
                            start = dest;
                            break;

                        case LineToCommand lineTo:
                            if (previous == null)
                            {
                                result.Append("L" + FormatCoordinate(dest.Diff.X) + " " + FormatCoordinate(dest.Diff.Y));
                            }
                            else if (dest.Diff.X == 0)
                            {
                                result.Append("v" + FormatCoordinate(dest.Diff.Y));
                            }
                            else if (dest.Diff.Y == 0)
                            {
                                result.Append("h" + FormatCoordinate(dest.Diff.X));
                            }
                            else
                            {
                                result.Append("l" + FormatCoordinate(dest.Diff.X) + " " + FormatCoordinate(dest.Diff.Y));
                            }
                            break;

                        case CurveToCommand curveTo:
                            var diffP1 = new PathPoint(previous, curveTo.X1, curveTo.Y1);
                            var diffP2 = new PathPoint(previous, curveTo.X2, curveTo.Y2);

                            result.Append((previous == null ? "C" : "c") +
                                FormatCoordinate(diffP1.Diff.X) + " " + FormatCoordinate(diffP1.Diff.Y) + "," +
                                FormatCoordinate(diffP2.Diff.X) + " " + FormatCoordinate(diffP2.Diff.Y) + "," +
                                FormatCoordinate(dest.Diff.X) + " " + FormatCoordinate(dest.Diff.Y));
                            break;

                        default:
                            throw new Exception("Unknown path command");
                    }

                    previous = dest;
                }
                else
                {
                    switch (command)
                    {
                        case ClosePathCommand _:
                            result.Append("z");
                            previous = start;
                            break;

                        default:
                            throw new Exception("Unknown path command");
                    }
                }
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
