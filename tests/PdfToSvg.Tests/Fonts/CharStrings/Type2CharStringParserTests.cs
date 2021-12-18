// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.CharStrings.Parser
{
    internal class PathConstructionOperators
    {
        [TestCase(42, 2, 2, 3, 3, "42 2 3 (21) 0 (6)", TestName = "rmoveto")]
        [TestCase(42, 2, 2, 0, 0, "42 2 (22) 0 (6)", TestName = "hmoveto")]
        [TestCase(42, 0, 0, 2, 2, "42 2 (4) 0 (6)", TestName = "vmoveto")]

        [TestCase(42, 0, 2, 0, 3, "42 2 3     (5)", TestName = "rlineto 3")]
        [TestCase(42, 0, 6, 0, 8, "42 2 3 4 5 (5)", TestName = "rlineto 5")]

        [TestCase(null, 0, 9, 0, 0, "9     (6)", TestName = "hlineto 1")]
        [TestCase(null, 0, 1, 0, 2, "1 2   (6)", TestName = "hlineto 2")]
        [TestCase(null, 0, 4, 0, 2, "1 2 3 (6)", TestName = "hlineto 3")]

        [TestCase(null, 0, 0, 0, 9, "9     (7)", TestName = "vlineto 1")]
        [TestCase(null, 0, 2, 0, 1, "1 2   (7)", TestName = "vlineto 2")]
        [TestCase(null, 0, 2, 0, 4, "1 2 3 (7)", TestName = "vlineto 3")]

        [TestCase(null, -4, 1, -2, 4, "    1 2 -5 -4 5 6                  (8)", TestName = "rrcurveto 6")]
        [TestCase(null, -4, 13, -2, 16, "  1 2 -5 -4 5 6   1 1 1 1 10 10  (8)", TestName = "rrcurveto 12")]
        [TestCase(42, -4, 1, -2, 4, "  42  1 2 -5 -4 5 6                  (8)", TestName = "rrcurveto 7")]
        [TestCase(42, -4, 13, -2, 16, "42  1 2 -5 -4 5 6   1 1 1 1 10 10  (8)", TestName = "rrcurveto 13")]

        [TestCase(null, -1, 3, -5, 0, "     1 2 -5 -4            (27)", TestName = "hhcurveto 4")]
        [TestCase(null, -1, 3, 0, 10, " 10  1 2 -5 -4            (27)", TestName = "hhcurveto 5")]
        [TestCase(null, -1, 18, -5, 2, "    1 2 -5 -4   5 6 7 8  (27)", TestName = "hhcurveto 8")]
        [TestCase(null, -1, 18, 0, 12, "10  1 2 -5 -4   5 6 7 8  (27)", TestName = "hhcurveto 9")]

        [TestCase(null, 0, 3, 0, 7, "   1 2 3 4                            (31)", TestName = "hvcurveto 4")]
        [TestCase(null, 0, 13, 0, 7, "  1 2 3 4                        10  (31)", TestName = "hvcurveto 5")]
        [TestCase(null, -3, 9, 0, 20, " 1 2 3 4  -1 -2 -3 -4  5 7 9 8      (31)", TestName = "hvcurveto 12")]
        [TestCase(null, -3, 19, 0, 20, "1 2 3 4  -1 -2 -3 -4  5 7 9 8  10  (31)", TestName = "hvcurveto 13")]

        [TestCase(null, -3, 3, 0, 7, "                   1 2 3 4  -1 -2 -2 -4      (31)", TestName = "hvcurveto 8")]
        [TestCase(null, -3, 3, 0, 14, "                  1 2 3 4  -1 -2 -2 -4  10  (31)", TestName = "hvcurveto 9")]
        [TestCase(null, -2, 4, 0, 7, " 1 0 0 0 0 0 0 0   1 2 3 4  -1 -2 -2 -4      (31)", TestName = "hvcurveto 16")]
        [TestCase(null, -2, 4, 0, 14, "1 0 0 0 0 0 0 0   1 2 3 4  -1 -2 -2 -4  10  (31)", TestName = "hvcurveto 17")]

        [TestCase(null, 0, 13, -4, 7, "               1 2 3 4 -1 -10  10 11  (24)", TestName = "rcurveline 8")]
        [TestCase(null, 0, 14, -4, 7, "  1 0 0 0 0 0  1 2 3 4 -1 -10  10 11  (24)", TestName = "rcurveline 14")]
        [TestCase(42, 0, 13, -4, 7, "42               1 2 3 4 -1 -10  10 11  (24)", TestName = "rcurveline 9")]
        [TestCase(42, 0, 14, -4, 7, "42  1 0 0 0 0 0  1 2 3 4 -1 -10  10 11  (24)", TestName = "rcurveline 15")]

        [TestCase(null, -10, 1, -11, 3, "  -10 -11        1 2 3 4 7 8  (25)", TestName = "rlinecurve 8")]
        [TestCase(null, -10, 2, -11, 3, "  -10 -11  1 0   1 2 3 4 7 8  (25)", TestName = "rlinecurve 10")]
        [TestCase(42, -10, 1, -11, 3, "42  -10 -11        1 2 3 4 7 8  (25)", TestName = "rlinecurve 9")]
        [TestCase(42, -10, 2, -11, 3, "42  -10 -11  1 0   1 2 3 4 7 8  (25)", TestName = "rlinecurve 11")]

        [TestCase(null, 0, 6, 0, 4, "   1 2 3 4                            (30)", TestName = "vhcurveto 4")]
        [TestCase(null, 0, 6, 0, 14, "  1 2 3 4                        10  (30)", TestName = "vhcurveto 5")]
        [TestCase(null, 0, 18, -3, 11, "1 2 3 4  -1 -2 -3 -4  5 7 9 8      (30)", TestName = "vhcurveto 12")]
        [TestCase(null, 0, 18, -3, 21, "1 2 3 4  -1 -2 -3 -4  5 7 9 8  10  (30)", TestName = "vhcurveto 13")]

        [TestCase(null, 0, 6, -2, 4, "                   1 2 3 4  -1 -2 -2 -4      (30)", TestName = "vhcurveto 8")]
        [TestCase(null, 0, 13, -2, 4, "                  1 2 3 4  -1 -2 -2 -4  10  (30)", TestName = "vhcurveto 9")]
        [TestCase(null, 0, 6, -1, 5, " 1 0 0 0 0 0 0 0   1 2 3 4  -1 -2 -2 -4      (30)", TestName = "vhcurveto 16")]
        [TestCase(null, 0, 13, -1, 5, "1 0 0 0 0 0 0 0   1 2 3 4  -1 -2 -2 -4  10  (30)", TestName = "vhcurveto 17")]

        [TestCase(null, 0, 12, -4, 1, "10           1 2 -1 -4  (26)", TestName = "vvcurveto 5")]
        [TestCase(null, 0, 12, -3, 2, "10  1 0 0 0  1 2 -1 -4  (26)", TestName = "vvcurveto 9")]
        [TestCase(null, 0, 2, -4, 1, "              1 2 -1 -4  (26)", TestName = "vvcurveto 4")]
        [TestCase(null, 0, 2, -3, 2, "     1 0 0 0  1 2 -1 -4  (26)", TestName = "vvcurveto 8")]

        [TestCase(null, -5, 25, -9, 21, "1 2 3 5 5 6  7 8 9 -10 -30 -20  (12_35)", TestName = "flex")]

        [TestCase(null, -3, 27, 0, 3, "1 2 3 7 8 9 -30  (12_34)", TestName = "hflex")]

        [TestCase(null, -7, 13, -7, 3, "1 1 1 2 3 7 1 -10 -20  (12_36)", TestName = "hflex1")]

        [TestCase(null, 0, 9, -4, 16, "1 1 1 2 3 7  1 2 3 4 -20  (12_37)", TestName = "flex1 abs(dx) < abs(dy)")]
        [TestCase(null, -2, 18, 0, 16, "1 1 1 2 3 7 10 2 3 4 -20  (12_37)", TestName = "flex1 abs(dx) > abs(dy)")]
        public void Exec(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            Helper.Operators(width, minx, maxx, miny, maxy, data);
        }
    }

    internal class FinishOperators
    {
        [TestCase(null, 0, 0, 0, 0, "0 (6) (14) 100 (6)", TestName = "endchar")]
        public void Exec(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            Helper.Operators(width, minx, maxx, miny, maxy, data);
        }
    }

    internal class HintOperators
    {
        [TestCase(42, 0, 0, 0, 0, "42  1 2 (1) 0 (6)", TestName = "hstem")]
        [TestCase(42, 0, 0, 0, 0, "42  1 2 3 4 (1) 0 (6)", TestName = "hstem")]

        [TestCase(42, 0, 0, 0, 0, "42  1 2 (3) 0 (6)", TestName = "vstem")]
        [TestCase(42, 0, 0, 0, 0, "42  1 2 3 4 (3) 0 (6)", TestName = "vstem")]

        [TestCase(42, 0, 0, 0, 0, "42  1 2 (18) 0 (6)", TestName = "hstemhm")]
        [TestCase(42, 0, 0, 0, 0, "42  1 2 3 4 (18) 0 (6)", TestName = "hstemhm")]

        [TestCase(42, 0, 0, 0, 0, "42  1 2 (23) 0 (6)", TestName = "vstemhm")]
        [TestCase(42, 0, 0, 0, 0, "42  1 2 3 4 (23) 0 (6)", TestName = "vstemhm")]

        [TestCase(42, 0, 93, 0, 0, "42  1 1 2 2 3 3 4 4 (18) 5 5 6 6 7 7 8 8     (19) 0xF7       93 (6)", TestName = "hstem + vstem + hintmask 8")]
        [TestCase(42, 0, 93, 0, 0, "42  1 1 2 2 3 3 4 4 (18) 5 5 6 6 7 7 8 8 9 9 (19) 0xF7 0xF7  93 (6)", TestName = "hstem + vstem + hintmask 9")]

        [TestCase(42, 0, 93, 0, 0, "42  1 1 2 2 3 3 4 4 5 5 6 6 7 7 8 8     (1)  (19) 0xF7       93 (6)", TestName = "hintmask 8")]
        [TestCase(42, 0, 93, 0, 0, "42  1 1 2 2 3 3 4 4 5 5 6 6 7 7 8 8 9 9 (1)  (19) 0xF7 0xF7  93 (6)", TestName = "hintmask 9")]

        [TestCase(42, 0, 93, 0, 0, "42  1 1 2 2 3 3 4 4 5 5 6 6 7 7 8 8     (1)  (20) 0xF7       93 (6)", TestName = "cntrmask 8")]
        [TestCase(42, 0, 93, 0, 0, "42  1 1 2 2 3 3 4 4 5 5 6 6 7 7 8 8 9 9 (1)  (20) 0xF7 0xF7  93 (6)", TestName = "cntrmask 9")]
        public void Exec(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            Helper.Operators(width, minx, maxx, miny, maxy, data);
        }
    }

    internal class ArithmeticOperators
    {
        [TestCase(null, 0, 2, 0, 0, "-2  (12_9) (6)", TestName = "abs -")]
        [TestCase(null, 0, 2, 0, 0, " 2  (12_9) (6)", TestName = "abs +")]
        [TestCase(null, 0, 5, 0, 0, "2 3 (12_10) (6)", TestName = "add")]
        [TestCase(null, -5, 0, 0, 0, "10 15 (12_11) (6)", TestName = "sub")]
        [TestCase(null, 0, 2, 0, 0, "20 10 (12_12) (6)", TestName = "div")]
        [TestCase(null, -20, 0, 0, 0, "20 (12_14) (6)", TestName = "neg")]
        [TestCase(null, 0, 0.42, 0, 0, "(12_23) (6)", TestName = "random")]
        [TestCase(null, 0, 21, 0, 0, "7 3 (12_24) (6)", TestName = "mul")]
        [TestCase(null, 0, 6, 0, 0, "36 (12_26) (6)", TestName = "sqrt")]
        [TestCase(null, 0, 75, 0, 0, "75 1 2 3 3 (12_18) (6)", TestName = "drop")]
        [TestCase(null, 0, 2, 0, 1, "1 2 (12_28) (5)", TestName = "exch")]
        [TestCase(1d, 6, 6, 0, 0, "1 2 3 4 5 6 7    1 (12_29)  (22) 0 (6)", TestName = "index")]
        [TestCase(1d, 7, 7, 0, 0, "1 2 3 4 5 6 7   -1 (12_29)  (22) 0 (6)", TestName = "index -")]
        [TestCase(1d, 5, 5, 0, 0, "1 2 3 4 5 6 7  3 2 (12_30)  (22) 0 (6)", TestName = "roll")]
        [TestCase(null, 0, 6, 0, 0, "3 (12_27) (12_10) (6)", TestName = "dup")]
        public void Exec(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            Helper.Operators(width, minx, maxx, miny, maxy, data);
        }
    }

    internal class StorageOperators
    {
        [TestCase(null, 0, 12, 0, 0, "1 1 (12_20) 12 2 (12_20) 17 3 (12_20) 2 (12_21) (6)", TestName = "put/get")]
        public void Exec(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            Helper.Operators(width, minx, maxx, miny, maxy, data);
        }
    }

    internal class ConditionalOperators
    {
        [TestCase(null, 0, 0, 0, 0, "0 0 (12_3) (6)", TestName = "and 0 && 0")]
        [TestCase(null, 0, 0, 0, 0, "5 0 (12_3) (6)", TestName = "and 5 && 0")]
        [TestCase(null, 0, 0, 0, 0, "0 5 (12_3) (6)", TestName = "and 0 && 5")]
        [TestCase(null, 0, 1, 0, 0, "5 5 (12_3) (6)", TestName = "and 5 && 5")]

        [TestCase(null, 0, 0, 0, 0, "0 0 (12_4) (6)", TestName = "or 0 || 0")]
        [TestCase(null, 0, 1, 0, 0, "5 0 (12_4) (6)", TestName = "or 5 || 0")]
        [TestCase(null, 0, 1, 0, 0, "0 5 (12_4) (6)", TestName = "or 0 || 5")]
        [TestCase(null, 0, 1, 0, 0, "5 5 (12_4) (6)", TestName = "or 5 || 5")]

        [TestCase(null, 0, 1, 0, 0, "0 (12_5) (6)", TestName = "not 0")]
        [TestCase(null, 0, 0, 0, 0, "5 (12_5) (6)", TestName = "not 5")]

        [TestCase(null, 0, 0, 0, 0, "0 5 (12_15) (6)", TestName = "eq 0 == 5")]
        [TestCase(null, 0, 1, 0, 0, "5 5 (12_15) (6)", TestName = "eq 5 == 5")]

        [TestCase(null, 0, 11, 0, 0, "11 22 4 5 (12_22) (6)", TestName = "ifelse v1 < v2")]
        [TestCase(null, 0, 11, 0, 0, "11 22 5 5 (12_22) (6)", TestName = "ifelse v1 == v2")]
        [TestCase(null, 0, 22, 0, 0, "11 22 6 5 (12_22) (6)", TestName = "ifelse v1 > v2")]
        public void Exec(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            Helper.Operators(width, minx, maxx, miny, maxy, data);
        }
    }

    internal class SubroutineOperators
    {
        [TestCase(null, 0, 79, 0, 0, "-106 (29) (6) 1 (6)", "gsubr:", "1 (11)", "78 (11)", TestName = "callgsubr")]
        [TestCase(null, 0, 79, 0, 0, "-106 (10) (6) 1 (6)", "subr:", " 1 (11)", "78 (11)", TestName = "callsubr")]

        public void Exec(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            Helper.Operators(width, minx, maxx, miny, maxy, data);
        }
    }

    internal class Helper
    {
        private static byte[] ParseSpec(string spec)
        {
            return spec
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(x =>
                {
                    if (x.StartsWith("("))
                    {
                        return x
                            .Trim('(', ')')
                            .Split('_')
                            .Select(x => byte.Parse(x, CultureInfo.InvariantCulture));
                    }

                    if (x.StartsWith("0x"))
                    {
                        return new[] { byte.Parse(x.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) };
                    }

                    var dbl = double.Parse(x, CultureInfo.InvariantCulture);
                    var fx = (int)(dbl * 0x10000);

                    return new byte[]
                    {
                        0xff,
                        (byte)(fx >> 24),
                        (byte)(fx >> 16),
                        (byte)(fx >> 8),
                        (byte)(fx >> 0)
                    };
                })
                .ToArray();
        }

        private static CharString ParseCharString(params string[] data)
        {
            var main = ParseSpec(data[0]);
            var globalSubrsData = new List<byte[]>();
            var localSubrsData = new List<byte[]>();

            var subrs = localSubrsData;

            for (var i = 1; i < data.Length; i++)
            {
                if (data[i] == "gsubr:")
                {
                    subrs = globalSubrsData;
                }
                else if (data[i] == "subr:")
                {
                    subrs = localSubrsData;
                }
                else
                {
                    subrs.Add(ParseSpec(data[i]));
                }
            }

            var globalSubrs = globalSubrsData
                .Select(x => new CharStringSubRoutine(x))
                .ToList();

            var localSubrs = localSubrsData
                .Select(x => new CharStringSubRoutine(x))
                .ToList();

            return Type2CharStringParser.Parse(new ArraySegment<byte>(main), globalSubrs, localSubrs);
        }

        public static void Operators(double? width, double minx, double maxx, double miny, double maxy, params string[] data)
        {
            var glyph = ParseCharString(data);

            Assert.AreEqual(
                minx + ", " + maxx + ", " + miny + ", " + maxy,
                glyph.MinX + ", " + glyph.MaxX + ", " + glyph.MinY + ", " + glyph.MaxY,
                "BBox");

            Assert.AreEqual(width, glyph.Width, "Width");
        }
    }
}
