// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Drawing
{
    public class SvgConversionTests
    {
        [TestCase(1, 0, 0, 1, 0, 0, "none")]
        [TestCase(1.000001, 0.000005, 0, 1, 0, 0, "none")]
        [TestCase(1, 0, 0, 1.000001, 1.100001, 1.111111111, "translate(1.1 1.1111)")]
        [TestCase(1, 0, 0, 2, 0, 0, "matrix(1 0 0 2 0 0)")]
        [TestCase(1, 2, 3, 4, 5, 6, "matrix(1 2 3 4 5 6)")]
        public void FormatMatrix(double a, double b, double c, double d, double e, double f, string expectedResult)
        {
            Assert.AreEqual(expectedResult, SvgConversion.Matrix(new Matrix(a, b, c, d, e, f)));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\r")]
        [TestCase("\n")]
        [TestCase("🙂")]
        [TestCase("a")]
        [TestCase("abc")]
        [TestCase("你好")]
        public void ReplaceInvalidChars_NoInvalidChars(string s)
        {
            var result = SvgConversion.ReplaceInvalidChars(s);
            Assert.IsTrue(ReferenceEquals(result, s));
        }

        [TestCase("\f", "#")]
        [TestCase(" \f", " #")]
        [TestCase("\f ", "# ")]
        [TestCase(" \f ", " # ")]
        [TestCase(" \f\f ", " ## ")]
        public void ReplaceInvalidChars_InvalidChars(string input, string expectedResult)
        {
            var actualResult = SvgConversion.ReplaceInvalidChars(input, '#');
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase("0001020304050607", "########")]
        [TestCase("08090A0B0C0D0E0F", "#\t\n##\r##")]
        [TestCase("1011121314151617", "########")]
        [TestCase("18191A1B1C1D1E1F", "########")]
        [TestCase("20", " ")]
        public void ReplaceInvalidChars_InvalidHexChars(string input, string expectedResult)
        {
            var decodedInput = new char[input.Length / 2];
            for (var i = 0; i < input.Length; i += 2)
            {
                decodedInput[i / 2] = (char)int.Parse(input.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            var actualResult = SvgConversion.ReplaceInvalidChars(new string(decodedInput), '#');
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
