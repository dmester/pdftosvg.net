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

        [TestCase(12341.234, "12341")]
        [TestCase(1234.1234, "1234")]
        [TestCase(123.41234, "123")]
        [TestCase(12.341234, "12.34")]
        [TestCase(1.2341234, "1.234")]
        [TestCase(.12341234, "0.1234")]
        [TestCase(.02341234, "0.0234")]
        public void FormatFontMetric(double input, string expectedResult)
        {
            Assert.AreEqual(expectedResult, SvgConversion.FormatFontMetric(input));
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

        [TestCase("00 01 02 03 04 05 06 07", "########")]
        [TestCase("08 09 0A 0B 0C 0D 0E 0F", "#\t\n##\r##")]
        [TestCase("10 11 12 13 14 15 16 17", "########")]
        [TestCase("18 19 1A 1B 1C 1D 1E 1F", "########")]
        [TestCase("20", " ")]
        public void ReplaceInvalidChars_InvalidHexChars(string input, string expectedResult)
        {
            var decodedInput = Encoding.ASCII.GetString(Hex.Decode(input));
            var actualResult = SvgConversion.ReplaceInvalidChars(decodedInput, '#');
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
