// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Drawing.Paths;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.Drawing
{
    /// <threadsafety instance="false" />
    internal class SvgConversion
    {
        private const string Hex = "0123456789abcdef";

        public const double CoordinateResolution = 0.0001;
        public const double TextOffsetResolution = 0.01;

        // long.MinValue: -9 223 372 036 854 775 808 => 20 chars + radix point + 'px' = 23 chars
        private readonly char[] buffer = new char[23];
        private StringBuilder? reusableSb;

        public string FormatColor(RgbColor color)
        {
            var r = (int)(color.Red * 255);
            var g = (int)(color.Green * 255);
            var b = (int)(color.Blue * 255);

            var buffer = this.buffer;

            // The last char is set first to allow the JIT to optimize away the range checks for the following accesses
            buffer[6] = Hex[b & 0xf];

            buffer[0] = '#';

            buffer[1] = Hex[(r >> 4) & 0xf];
            buffer[2] = Hex[r & 0xf];

            buffer[3] = Hex[(g >> 4) & 0xf];
            buffer[4] = Hex[g & 0xf];

            buffer[5] = Hex[(b >> 4) & 0xf];

            if (buffer[1] == buffer[2] &&
                buffer[3] == buffer[4] &&
                buffer[5] == buffer[6])
            {
                buffer[2] = buffer[3];
                buffer[3] = buffer[5];
                return new string(buffer, 0, 4);
            }
            else
            {
                return new string(buffer, 0, 7);
            }
        }

        private static bool IsInvalidChar(char ch)
        {
            // Invalid XML char according to 
            // https://www.w3.org/TR/REC-xml/#charsets
            return
                ch >= '\ufffe' ||
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

        public string? FormatFontStyle(FontStyle? fontStyle)
        {
            return fontStyle switch
            {
                FontStyle.Italic => "italic",
                FontStyle.Oblique => "oblique",
                _ => null,
            };
        }

        public string? FormatFontWeight(FontWeight? fontWeight)
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

        public string FormatFontMetricPx(double number)
        {
            int maxDecimalDigits;
            int multiplier;

            if (number >= 100 || number <= -100)
            {
                maxDecimalDigits = 0;
                multiplier = 1;
            }
            else if (number >= 10 || number <= -10)
            {
                maxDecimalDigits = 2;
                multiplier = 100;
            }
            else if (number >= 1 || number <= -1)
            {
                maxDecimalDigits = 3;
                multiplier = 1000;
            }
            else
            {
                maxDecimalDigits = 4;
                multiplier = 10000;
            }

            var buffer = this.buffer;
            var startIndex = FormatNumberCore(buffer, buffer.Length - 2, number, maxDecimalDigits, multiplier);

            if (startIndex < 0)
            {
                return "0";
            }
            else
            {
                // Will always be true, but improves JIT range check optimization
                if (buffer.Length >= 2)
                {
                    buffer[buffer.Length - 2] = 'p';
                    buffer[buffer.Length - 1] = 'x';
                }

                return new string(buffer, startIndex, buffer.Length - startIndex);
            }
        }

        public string FormatCoordinate(double number)
        {
            return FormatNumber(number, 4, 10000);
        }

        public void FormatCoordinate(StringBuilder sb, double number)
        {
            FormatNumber(sb, number, 4, 10000);
        }

        public string FormatCoordinatePx(double number)
        {
            var buffer = this.buffer;
            var startIndex = FormatNumberCore(buffer, buffer.Length - 2, number, 4, 10000);

            if (startIndex < 0)
            {
                return "0";
            }
            else
            {
                // Will always be true, but improves JIT range check optimization
                if (buffer.Length >= 2)
                {
                    buffer[buffer.Length - 2] = 'p';
                    buffer[buffer.Length - 1] = 'x';
                }

                return new string(buffer, startIndex, buffer.Length - startIndex);
            }
        }

        public string FormatTextOffset(double number)
        {
            return FormatNumber(number, 2, 100);
        }

        public string FormatBlendMode(BlendMode mode)
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

        private void FormatNumber(StringBuilder sb, double number, int maxDecimalDigits, int multiplier)
        {
            var buffer = this.buffer;
            var startIndex = FormatNumberCore(buffer, buffer.Length, number, maxDecimalDigits, multiplier);

            if (startIndex < 0)
            {
                sb.Append('0');
            }
            else
            {
                sb.Append(buffer, startIndex, buffer.Length - startIndex);
            }
        }

        private string FormatNumber(double number, int maxDecimalDigits, int multiplier)
        {
            var buffer = this.buffer;
            var startIndex = FormatNumberCore(buffer, buffer.Length, number, maxDecimalDigits, multiplier);

            if (startIndex < 0)
            {
                return "0";
            }
            else
            {
                return new string(buffer, startIndex, buffer.Length - startIndex);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static int FormatNumberCore(
            char[] buffer, int bufferEndIndex,
            double number, int maxDecimalDigits, int multiplier)
        {
            if (!double.IsNaN(number))
            {
                number = Math.Round(number * multiplier);

                // Saturate scaled value
                var scaledNumber =
                    number <= -long.MaxValue ? -long.MaxValue : // long.MinValue will not negate
                    number >= long.MaxValue ? long.MaxValue :
                    (long)number;

                if (scaledNumber != 0)
                {
                    var startIndex = bufferEndIndex;

                    var negative = scaledNumber < 0;
                    if (negative)
                    {
                        scaledNumber = -scaledNumber;
                    }

                    // Decimal digits
                    while (maxDecimalDigits-- > 0)
                    {
                        var remainder = (int)(scaledNumber % 10);
                        scaledNumber /= 10;

                        if (remainder != 0)
                        {
                            // First non-zero decimal
                            buffer[--startIndex] = (char)('0' + remainder);

                            // Remaining decimals
                            while (maxDecimalDigits-- > 0)
                            {
                                remainder = (int)(scaledNumber % 10);
                                scaledNumber /= 10;

                                buffer[--startIndex] = (char)('0' + remainder);
                            }

                            buffer[--startIndex] = '.';
                            break;
                        }
                    }

                    // Integer digits
                    while (scaledNumber != 0)
                    {
                        var remainder = (int)(scaledNumber % 10);
                        scaledNumber /= 10;

                        buffer[--startIndex] = (char)('0' + remainder);
                    }

                    if (negative)
                    {
                        buffer[--startIndex] = '-';
                    }

                    return startIndex;
                }
            }

            return -1;
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

        public string PathData(PathData path)
        {
            var result = reusableSb;
            if (result == null)
            {
                reusableSb = result = new StringBuilder();
            }
            else
            {
                result.Clear();
            }

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
                                result.Append('M');
                                FormatCoordinate(result, dest.Diff.X);
                                result.Append(' ');
                                FormatCoordinate(result, dest.Diff.Y);
                            }
                            else
                            {
                                result.Append('m');
                                FormatCoordinate(result, dest.Diff.X);
                                result.Append(' ');
                                FormatCoordinate(result, dest.Diff.Y);
                            }
                            start = dest;
                            break;

                        case LineToCommand lineTo:
                            if (previous == null)
                            {
                                result.Append('L');
                                FormatCoordinate(result, dest.Diff.X);
                                result.Append(' ');
                                FormatCoordinate(result, dest.Diff.Y);
                            }
                            else if (dest.Diff.X == 0)
                            {
                                result.Append('v');
                                FormatCoordinate(result, dest.Diff.Y);
                            }
                            else if (dest.Diff.Y == 0)
                            {
                                result.Append('h');
                                FormatCoordinate(result, dest.Diff.X);
                            }
                            else
                            {
                                result.Append('l');
                                FormatCoordinate(result, dest.Diff.X);
                                result.Append(' ');
                                FormatCoordinate(result, dest.Diff.Y);
                            }
                            break;

                        case CurveToCommand curveTo:
                            var diffP1 = new PathPoint(previous, curveTo.X1, curveTo.Y1);
                            var diffP2 = new PathPoint(previous, curveTo.X2, curveTo.Y2);

                            result.Append(previous == null ? 'C' : 'c');
                            FormatCoordinate(result, diffP1.Diff.X);
                            result.Append(' ');
                            FormatCoordinate(result, diffP1.Diff.Y);
                            result.Append(',');
                            FormatCoordinate(result, diffP2.Diff.X);
                            result.Append(' ');
                            FormatCoordinate(result, diffP2.Diff.Y);
                            result.Append(',');
                            FormatCoordinate(result, dest.Diff.X);
                            result.Append(' ');
                            FormatCoordinate(result, dest.Diff.Y);
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
                            result.Append('z');
                            previous = start;
                            break;

                        default:
                            throw new Exception("Unknown path command");
                    }
                }
            }

            return result.ToString();
        }

        public string Matrix(Matrix matrix)
        {
            var a = FormatCoordinate(matrix.A);
            var b = FormatCoordinate(matrix.B);
            var c = FormatCoordinate(matrix.C);
            var d = FormatCoordinate(matrix.D);
            var e = FormatCoordinate(matrix.E);
            var f = FormatCoordinate(matrix.F);

            if (b == "0" && c == "0")
            {
                if (a == "1" && d == "1")
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
                else if (e == "0" && f == "0")
                {
                    if (a == d)
                    {
                        return "scale(" + a + ")";
                    }
                    else
                    {
                        return "scale(" + a + " " + d + ")";
                    }
                }
            }

            return "matrix(" + a + " " + b + " " + c + " " + d + " " + e + " " + f + ")";
        }
    }
}
